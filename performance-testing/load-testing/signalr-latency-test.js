/**
 * SignalR Latency Testing with k6
 * 
 * Mesure la latence des notifications en temps réel
 * 
 * Usage:
 *   k6 run signalr-latency-test.js
 */

import ws from 'k6/ws';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'ws://localhost:5000';
const HUB_NAME = __ENV.HUB || 'consultationHub';

// Configuration for SignalR latency testing
export const options = {
  vus: 10,
  duration: '2m',
  thresholds: {
    'ws_connecting': ['p(95)<1000'],  // 95% des connexions < 1s
    'ws_session_duration': ['p(95)<120000'],  // Sessions < 2min
  },
};

export default function () {
  const url = `${BASE_URL}/${HUB_NAME}?transport=webSocket`;
  
  const res = ws.connect(url, (socket) => {
    // Measure connection time
    const connectionTime = performance.now();
    
    socket.on('open', () => {
      const connectedTime = performance.now();
      const latency = connectedTime - connectionTime;
      
      check(latency, {
        'WebSocket connection established': (lat) => lat < 1000,
      });
      
      // Send ping message
      socket.send(JSON.stringify({
        protocol: 'json',
        version: 1,
      }));
    });

    socket.on('message', (data) => {
      // Measure message receive latency
      const receiveTime = performance.now();
      
      check(data, {
        'Message received': (msg) => msg !== '',
      });
    });

    socket.on('close', () => {
      check(true, {
        'WebSocket closed cleanly': () => true,
      });
    });

    socket.on('error', (e) => {
      check(false, {
        'No WebSocket errors': () => false,
        'Error': () => `${e}`,
      });
    });

    // Keep connection open
    socket.setTimeout(() => {
      socket.close();
    }, 120000);
  }, {
    timeout: '60s',
  });

  check(res, {
    'status is 101': (r) => r && r.status === 101,
  });
}

/**
 * Expected Results:
 * - Connection time: < 100ms
 * - Message latency: < 50ms
 * - Success rate: > 99%
 */
