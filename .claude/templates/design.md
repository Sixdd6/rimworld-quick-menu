# Design: <name>

- **Status**: draft | approved (YYYY-MM-DD)
- **Spec**: [spec.md](spec.md) · **Assets**: [assets.md](assets.md)
- Sections that do not apply (for example, Audio for a silent CLI) say "N/A because …".

## 1. UX
### Information architecture
<!-- Screens/views/commands and how they connect (Mermaid flowchart). -->
### Key screens and states
<!-- For each screen: purpose, main elements, empty / loading / error / success states. Link mockups in design/. -->
### Responsive behaviour
<!-- Breakpoints and what changes at each one. -->

## 2. Design system
### Color tokens
| Token | Light | Dark | Use | Contrast (vs bg) |
|---|---|---|---|---|
### Typography
<!-- Font families (with license), type scale, weights, line heights. -->
### Spacing, radius, elevation
### Motion
<!-- Durations, easing, what animates; reduced-motion behaviour. -->
### Components
<!-- Core components and their states (default, hover, focus, active, disabled, error). -->

## 3. Art direction
<!-- Style, palette, references, resolution/scale, animation approach. Link reference images. -->

## 4. Audio direction
<!-- Mood, style references, role of audio. -->
### Sound list
| ID | Sound | Trigger | Duration | Loop | Variations | Priority |
|---|---|---|---|---|---|---|
### Technical spec
<!-- Sample rate, formats per platform, channels, loudness target (LUFS / dBTP), size budget, preload vs stream. -->
### Mixing
<!-- Categories/buses, default volumes, ducking, max voices. -->

## 5. Accessibility
<!-- WCAG target level, contrast, focus, keyboard, screen reader, captions, reduced motion, audio alternatives. -->

## 6. Asset strategy and pipeline
| Asset type | Strategy (code / SVG / library / generated / user-supplied) | Tool or source | Cost |
|---|---|---|---|
<!-- Source vs export directories, naming, formats, compression and conversion scripts. -->
