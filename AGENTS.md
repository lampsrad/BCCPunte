# AGENTS.md

Project rules for Grok (and other agents) working in this repository.

Architecture, commands, and layer overview live in `CLAUDE.md`. Follow both files when they apply.

## Don't approximate (mandatory)

When the user asks to match **Project Viewer** (or any other reference project/file):

1. **Open the real source first** — markup, CSS, and code-behind for that feature. Do not implement from memory or summary alone.
2. **Copy the interaction model and structure**, not a “BCC-shaped” reimplementation.
   - Example: Viewer Help_Merge is a **side flyout panel** on hover/focus of Help — **not** a separate Admin menu item, and **not** a nested `<ul>` submenu dropdown.
3. **Do not invent a close equivalent.** If the UI shell differs, still preserve trigger, placement, open/close behavior, and control hierarchy.
4. **Before finishing**, re-read the reference and verify: same parent control, same secondary control type (flyout vs submenu vs separate item), same user path (click vs hover).
5. If something cannot match 1:1, **say so and ask** — never silently approximate.

Same rule applies to “same as Viewer Help”, “same styling as X”, “do it like the other project”, etc.
