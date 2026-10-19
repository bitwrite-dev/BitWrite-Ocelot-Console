/**
 * Read-only JSON viewer for configuration previews.
 *
 * Renders with react-syntax-highlighter's light build and GitHub's own dark
 * palette (the colours github.com uses for JSON in dark mode), which is what
 * an operator reading a configuration document expects. The build is the
 * low-level one and JSON is registered on it, so the bundle carries one
 * language rather than every language the library knows.
 */

import type { CSSProperties } from 'react'
import { Light as SyntaxHighlighter } from 'react-syntax-highlighter'
import json from 'react-syntax-highlighter/dist/esm/languages/hljs/json'

SyntaxHighlighter.registerLanguage('json', json)

/** GitHub's code font stack, so the block reads like the file it mirrors. */
const CODE_FONT =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace'

/**
 * GitHub dark, as the hljs token classes the light renderer applies.
 *
 * The library ships no `github-dark` theme, so the Primer dark palette is
 * spelled out here: canvas for the surface, default text for punctuation, the
 * blue the site uses for keys, numbers and literals, and red for keywords.
 */
const GITHUB_DARK: Record<string, CSSProperties> = {
  hljs: { color: '#c9d1d9', background: '#0d1117' },
  'hljs-comment': { color: '#8b949e', fontStyle: 'italic' },
  'hljs-keyword': { color: '#ff7b72' },
  'hljs-string': { color: '#a5d6ff' },
  'hljs-number': { color: '#79c0ff' },
  'hljs-literal': { color: '#79c0ff' },
  'hljs-attr': { color: '#79c0ff' },
  'hljs-variable': { color: '#ffa657' },
  'hljs-punctuation': { color: '#c9d1d9' },
  'hljs-meta': { color: '#8b949e' },
}

/** Pretty-prints when the payload parses, and shows it verbatim when it does not. */
function format(payload: string): string {
  try {
    return JSON.stringify(JSON.parse(payload), null, 2)
  } catch {
    return payload
  }
}

export function JsonHighlight({ payload }: { payload: string }) {
  const text = format(payload)

  return (
    <SyntaxHighlighter
      language="json"
      style={GITHUB_DARK}
      showLineNumbers
      wrapLongLines
      customStyle={{
        margin: 0,
        padding: '0.75rem',
        borderRadius: '0.375rem',
        border: '1px solid #30363d',
        fontSize: '0.75rem',
        lineHeight: 1.6,
        fontFamily: CODE_FONT,
      }}
      lineNumberStyle={{
        color: '#8b949e',
        paddingRight: '0.75rem',
        minWidth: '2.5em',
        textAlign: 'right',
        userSelect: 'none',
      }}
    >
      {text}
    </SyntaxHighlighter>
  )
}

/**
 * Copies text to the clipboard, falling back for browsers that withhold the
 * async clipboard API outside a secure context.
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
      return true
    }
  } catch {
    // Fall through to the legacy path below.
  }

  try {
    const area = document.createElement('textarea')
    area.value = text
    area.setAttribute('readonly', '')
    area.style.position = 'fixed'
    area.style.opacity = '0'
    document.body.appendChild(area)
    try {
      area.select()
      return document.execCommand('copy')
    } finally {
      // Removed even when execCommand throws, so a failed copy does not leave a
      // hidden element behind in the document.
      document.body.removeChild(area)
    }
  } catch {
    return false
  }
}
