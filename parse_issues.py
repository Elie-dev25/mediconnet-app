import json
import sys

severity = sys.argv[1] if len(sys.argv) > 1 else 'BLOCKER'
with open(f'{severity.lower()}s.json', 'r') as f:
    d = json.load(f)
    
print(f'{severity}S: {d["total"]}')
for i in d['issues']:
    file = i['component'].split(':')[-1]
    line = i.get('line', '')
    rule = i['rule']
    msg = i['message'][:70]
    print(f'{file}:{line} - {rule} - {msg}')
