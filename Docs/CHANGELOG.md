# Image Lightness Organizer Change Log 📋

## v1.0.2 **(current)** 🆕
- Fixed inconsistent output folder names for the same lightness range (e.g., "Light 00%-10%" and "Light 00-10%"). All folders now use the "Light XX%-YY%" format.
- Fixed overlapping output folder ranges when using percentage steps that are not divisors of 100 (3, 6, 7, 8 and 9). The last range is now capped to 100%.
- Fixed semi-transparent pixels in WebP, ICO and AVIF images being measured darker than they actually are.
- Fixed the alpha threshold so that only pixels strictly below the threshold are excluded from the lightness average.
- Fixed inaccurate "Success" and "Failed" totals in the final summary when processing files in parallel.
- WebP, ICO and AVIF images are now decoded only once, and decoding errors are always reported.
- Minor source-code changes. Added a unit and integration test project.

## v1.0.1 🔄
- Added an optional argument (1–10) to specify the lightness percentage increments for folder names (default remains at 10%).
- Minor source-code changes.

## v1.0 🔄
Initial Release.