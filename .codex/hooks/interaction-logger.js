'use strict';

const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

function readEvent() {
  return JSON.parse(fs.readFileSync(0, 'utf8'));
}

function safeText(value, maximumLength) {
  return String(value || '')
    .replace(/ChangeMe123!/g, '[REDACTED]')
    .replace(/\b(password|passwd|secret|token|api[-_ ]?key|authorization|cookie|private[-_ ]?key|connection[-_ ]?string)\b(\s*[:=]\s*)(?:"[^"]*"|'[^']*'|[^\s,;]+)/gi, '$1$2[REDACTED]')
    .replace(/\bBearer\s+[A-Za-z0-9._~+/=-]+/gi, 'Bearer [REDACTED]')
    .replace(/\b(?:gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9_-]{20,}|AKIA[A-Z0-9]{16})\b/g, '[REDACTED]')
    .replace(/[\r\n\t]+/g, ' ')
    .replace(/\s{2,}/g, ' ')
    .trim()
    .slice(0, maximumLength);
}

function eventKey(event) {
  const sessionId = event.session_id || event.sessionId || 'session-unavailable';
  const turnId = event.turn_id || event.turnId || 'turn-unavailable';
  return crypto.createHash('sha256').update(`${sessionId}:${turnId}`).digest('hex');
}

function pendingFile(event) {
  return path.join(os.tmpdir(), 'codex-ai-interaction-pending', `${eventKey(event)}.json`);
}

function savePrompt(event) {
  const prompt = safeText(event.prompt, 12000);
  if (!prompt) return;

  const filePath = pendingFile(event);
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify({
    prompt,
    turnId: safeText(event.turn_id || event.turnId || 'unavailable', 160),
  }), 'utf8');
}

function appendResult(event) {
  const filePath = pendingFile(event);
  if (!fs.existsSync(filePath)) return;

  const pending = JSON.parse(fs.readFileSync(filePath, 'utf8'));
  const result = safeText(event.last_assistant_message || event.lastAssistantMessage, 12000) || '[No final assistant message was provided]';
  const repoRoot = execFileSync('git', ['rev-parse', '--show-toplevel'], {
    cwd: process.cwd(),
    encoding: 'utf8',
  }).trim();
  const logPath = path.join(repoRoot, 'docs', 'ai-interaction-log.md');
  const entry = [
    `## Interaction ${new Date().toISOString()}`,
    '',
    `- Prompt/goal: ${pending.prompt}`,
    '- Agent/tool: Codex project hook (UserPromptSubmit + Stop)',
    `- Result: ${result}`,
    '- Verification: Pending manual entry; this hook does not observe tool/test outcomes.',
    '- Accepted/changed: Pending manual review; record accepted decisions or changed files.',
    `- Turn: ${pending.turnId}`,
    '',
  ].join('\n');

  fs.mkdirSync(path.dirname(logPath), { recursive: true });
  fs.appendFileSync(logPath, `${entry}\n`, 'utf8');
  fs.unlinkSync(filePath);
}

try {
  const event = readEvent();
  if (event.hook_event_name === 'UserPromptSubmit') savePrompt(event);
  if (event.hook_event_name === 'Stop') appendResult(event);
} catch (error) {
  // Logging must not interrupt the user's coding session on malformed hook input.
  process.stderr.write(`AI interaction log hook skipped an event: ${error.message}\n`);
}
