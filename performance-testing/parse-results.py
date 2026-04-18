#!/usr/bin/env python3
"""
Helper script to update readme-result.md with real metrics from performance tests
This is an alternative to manual updates
"""

import json
import glob
import os
from datetime import datetime
from pathlib import Path

def find_latest_results():
    """Find the most recent test result files"""
    results_dir = Path("results")
    
    frontend_files = sorted(results_dir.glob("frontend-metrics-*.json"), reverse=True)
    backend_files = sorted(results_dir.glob("backend-metrics-*.json"), reverse=True)
    k6_files = sorted(results_dir.glob("k6-results-*.json"), reverse=True)
    
    return {
        'frontend': frontend_files[0] if frontend_files else None,
        'backend': backend_files[0] if backend_files else None,
        'k6': k6_files[0] if k6_files else None,
    }

def load_metrics(filepath):
    """Load metrics from JSON file"""
    if not filepath:
        return None
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading {filepath}: {e}")
        return None

def generate_metrics_snippet(metrics):
    """Generate markdown snippet from metrics"""
    
    if not metrics:
        return None
    
    frontend = metrics.get('frontend')
    backend = metrics.get('backend')
    
    snippet = f"""
## 🚀 Real Performance Metrics (Updated {datetime.now().strftime('%Y-%m-%d %H:%M:%S')})

### Frontend Performance
"""
    
    if frontend:
        build_time = frontend.get('buildTimeSeconds', 'N/A')
        bundle_size = frontend.get('bundleSizeKB', 'N/A')
        gzip_size = frontend.get('gzipSizeKB', 'N/A')
        prod_deps = frontend.get('productionDependencies', 'N/A')
        
        snippet += f"""
- **Build Time**: {build_time}s
- **Bundle Size**: {bundle_size} KB
- **Gzipped**: {gzip_size} KB
- **Production Dependencies**: {prod_deps}
"""
    
    if backend:
        stats = backend.get('statistics', {})
        avg_response = stats.get('avgResponseTimeMs', 'N/A')
        success_rate = stats.get('successRatePercent', 'N/A')
        
        snippet += f"""
### Backend API Performance

- **Average Response Time**: {avg_response} ms
- **Success Rate**: {success_rate}%
"""
    
    return snippet

def main():
    print("📊 Performance Metrics Parser")
    print("=" * 50)
    
    # Find latest results
    results = find_latest_results()
    
    if not any(results.values()):
        print("❌ No test results found in results/ directory")
        print("Run tests first: .\\run-all-tests.ps1 -All")
        return
    
    # Load metrics
    metrics = {}
    for key, filepath in results.items():
        if filepath:
            print(f"📄 Loading {key} metrics from {filepath.name}")
            metrics[key] = load_metrics(filepath)
    
    # Generate snippet
    snippet = generate_metrics_snippet(metrics)
    
    if snippet:
        print("\n" + snippet)
        print("\n" + "=" * 50)
        print("Copy this snippet to readme-result.md")
        
        # Save to file
        output_file = Path("results/metrics-snippet.md")
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(snippet)
        print(f"✅ Snippet saved to {output_file}")

if __name__ == "__main__":
    main()
