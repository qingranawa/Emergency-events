const fs = require('fs');

const assemblyPath = 'D:/PROGRA~2/Steam/steamapps/common/SCPSEC~2/SCPSL_Data/Managed/Assembly-CSharp.dll';
const bytes = fs.readFileSync(assemblyPath);

const ascii = bytes.toString('latin1').match(/[ -~]{4,}/g) ?? [];
const utf16 = bytes.toString('utf16le').match(/[ -~]{4,}/g) ?? [];
const strings = [...new Set([...ascii, ...utf16])]
  .filter((value) => /SpawnDummyCommand|spawn dummy|dummy spawn|dummy(s)?|action dummy|follow dummy|destroy dummy/i.test(value))
  .sort((left, right) => left.localeCompare(right));

for (const value of strings) {
  console.log(value);
}

const marker = Buffer.from('SpawnDummyCommand', 'utf8');
let offset = bytes.indexOf(marker);
if (offset >= 0) {
  const window = bytes.subarray(Math.max(0, offset - 3000), Math.min(bytes.length, offset + 3000));
  const nearby = [
    ...(window.toString('latin1').match(/[ -~]{3,}/g) ?? []),
    ...(window.toString('utf16le').match(/[ -~]{3,}/g) ?? []),
  ];
  console.log('--- nearby SpawnDummyCommand strings ---');
  for (const value of [...new Set(nearby)]) {
    console.log(value);
  }
}
