# SVG Icons Organization

All SVG icons have been extracted from inline HTML and organized into a central folder structure for better maintainability and reusability.

## Folder Structure

```
sarah.client/src/assets/icons/
├── Action Icons (for buttons)
│   ├── add.svg          - Plus icon for "Add" buttons
│   ├── edit.svg         - Pencil icon for "Edit" buttons
│   ├── delete.svg       - Trash icon for "Delete" buttons
│   ├── refresh.svg      - Circular arrow for "Refresh" buttons
│   ├── star.svg         - Star icon for "Favorite" action
│   └── unfavorite.svg   - Tool icon for "Unfavorite" action
│
├── Entity Icons (for cards and lists)
│   ├── lightbulb.svg    - Light bulb for lamp/light devices
│   ├── plug.svg         - Power plug for outlet devices
│   ├── location.svg     - Location pin for GPS trackers
│   ├── settings.svg     - Gear icon for generic devices
│   ├── play.svg         - Play button for scenes
│   ├── home.svg         - House icon for rooms
│   └── person.svg       - Person silhouette for persons
│
└── Hero/Illustration SVGs
    ├── hero-smart-home.svg  - Main banner illustration
    ├── device-hero.svg      - Device category illustration
    ├── person-hero.svg      - Person category illustration
    └── room-hero.svg        - Room category illustration
```

## Usage

All SVG icons are now referenced as external files using `<img>` tags:

### Large Icons (48x48px)
```html
<img class="big-icon" src="assets/icons/lightbulb.svg" alt="Lightbulb" title="Lamp">
```

### Small Icons (16x16px and 20x20px)
```html
<img src="assets/icons/refresh.svg" alt="Refresh" style="width: 20px; height: 20px;">
<img src="assets/icons/edit.svg" alt="Edit" style="width: 16px; height: 16px;">
```

## Benefits

1. **Maintainability**: Icons are defined once and can be updated globally
2. **Reusability**: Same icon can be used across multiple components
3. **Performance**: Browser can cache SVG files separately
4. **Cleaner HTML**: No large inline SVG code cluttering the templates
5. **Version Control**: Changes to icons are clearly tracked in git
6. **Flexibility**: Easy to swap icons without touching component code

## Color Handling

Icons use CSS filters to apply colors based on context:

- **Device cards**: Dark gray color (filter applied)
- **Person/Room/Scene cards**: White color (filter applied)
- **Buttons**: Inherit color from button style

## Migration Notes

All inline SVG elements have been replaced with `<img>` tags. The SVG files retain the same viewBox and path definitions for visual consistency. CSS filters are used to colorize the icons dynamically.

## Adding New Icons

1. Create a new SVG file in `assets/icons/`
2. Use the same format as existing icons (viewBox="0 0 24 24")
3. Reference it using `<img src="assets/icons/your-icon.svg">`
4. Add appropriate CSS styling if needed
