from collections import Counter, defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET

root = ET.parse('/home/ubuntu/upload/issues.xml').getroot()
issues = []
for project in root.findall('./Issues/Project'):
    project_name = project.attrib.get('Name', '<unknown>')
    for issue in project.findall('Issue'):
        row = dict(issue.attrib)
        row['Project'] = project_name
        issues.append(row)

type_counts = Counter(row.get('TypeId', '<unknown>') for row in issues)
project_counts = Counter(row.get('Project', '<unknown>') for row in issues)
severity_by_type = {}
for issue_type in root.findall('./IssueTypes/IssueType'):
    severity_by_type[issue_type.attrib.get('Id', '<unknown>')] = issue_type.attrib.get('Severity', '<unknown>')

out = []
out.append(f'TotalIssues: {len(issues)}')
out.append('ProjectCounts:')
for name, count in project_counts.most_common():
    out.append(f'  {name}: {count}')
out.append('IssueTypeCounts:')
for name, count in type_counts.most_common():
    out.append(f'  {name}: {count} severity={severity_by_type.get(name, "<missing-type-definition>" )}')
out.append('HighPriorityIssues:')
for issue in issues:
    severity = severity_by_type.get(issue.get('TypeId', ''), '<missing-type-definition>')
    if severity in {'ERROR', 'WARNING'}:
        out.append('  ' + ' | '.join([
            issue.get('TypeId', ''), severity, issue.get('Project', ''),
            issue.get('File', ''), f"line={issue.get('Line', '')}", issue.get('Message', '')
        ]))
Path('/home/ubuntu/Ricis.Core/ISSUES_XML_ANALYSIS_2026-08-20.md').write_text('\n'.join(out) + '\n', encoding='utf-8')
print('\n'.join(out[:80]))
