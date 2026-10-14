"""Check every design cell and reference, then generate one descriptor per row."""
import json, re, sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
tables = {p.stem: json.loads(p.read_text()) for p in sorted((root/'sheets').glob('*.json'))}
errors=[]
cells=0
for name, sheet in tables.items():
    ids=set()
    for row in sheet['rows']:
        rid=row.get('id','')
        if not rid or rid in ids: errors.append(f'{name}: missing or duplicate id {rid}')
        ids.add(rid)
        for col in sheet['columns']:
            cells+=1
            if col not in row or row[col] is None or row[col]=='': errors.append(f'{name}.{rid}.{col}: unfilled')
            if col not in sheet.get('checked',{}).get(rid,[]): errors.append(f'{name}.{rid}.{col}: unverified specification')
        for col, target in sheet.get('references',{}).items():
            if target not in tables or row.get(col) not in {r['id'] for r in tables[target]['rows']}:
                errors.append(f'{name}.{rid}.{col}: unresolved {target}/{row.get(col)}')
        if name=='hooks':
            import os
            reference_root=Path(os.environ.get('ULTRACRAFT_REFERENCE_ROOT',str(root.parents[1]/'work')))
            source=reference_root/row['source']
            if not source.is_file() or row['member'] not in source.read_text(encoding='utf-8-sig'):
                errors.append(f'{name}.{rid}: hook source does not verify member')
report={'specificationCells':cells,'errors':errors,'clean':not errors,'note':'Checks specification completeness. Runtime acceptance tests are reported independently.'}
(root/'preflight.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
if errors: sys.exit(1)
if '--generate' not in sys.argv: sys.exit(0)
def pascal(value): return ''.join(w.capitalize() for w in re.split('[^a-zA-Z0-9]',value))
cs=['// Generated from checked JSON sheets. Do not edit.','namespace UltraCraft.Generated;','internal static class Design {']
java=['// Generated from checked JSON sheets. Do not edit.','package dev.skycraft;','public final class Design {']
for name, sheet in tables.items():
    for row in sheet['rows']:
        cname=pascal(name)+'_'+pascal(row['id'])
        cs.append(f'  internal static class {cname} {{')
        java.append(f'  public static final class {cname} {{')
        for field, value in row.items():
            typename='bool' if isinstance(value,bool) else 'int' if isinstance(value,int) else 'double' if isinstance(value,float) else 'string'
            literal=json.dumps(value,ensure_ascii=True)
            cs.append(f'    public const {typename} {pascal(field)} = {literal};')
            jt={'bool':'boolean','string':'String'}.get(typename,typename)
            java.append(f'    public static final {jt} {pascal(field)} = {literal};')
        cs.append('  }'); java.append('  }')
cs.append('}');java.append('}')
for path, lines in [(root/'host'/'Generated'/'Design.cs',cs),(root/'java'/'src'/'main'/'java'/'dev'/'skycraft'/'Design.java',java)]:
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text('\n'.join(lines)+'\n')
