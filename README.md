<!-- Common Project Tags:
command-line 
console-applications 
desktop-app 
desktop-application 
dotnet 
dotnet-core 
netcore 
netframework 
netframework48 
tool 
tools 
vbnet 
visualstudio 
windows 
windows-app 
windows-application 
windows-applications 
windows-forms 
winforms 
images 
image-files 
imagefiles 
classification 
organizer 
directories 
bmp 
bmps 
bitmap 
bitmaps 
jpg 
jpgs 
jpeg 
jpegs 
png 
pngs 
tiff 
webp 
light 
lightness 
files 
image-processing 
image-classification 
 -->

<div align="center">
  <img src="/blob/main/Images/App.ico" width="80" alt="FIV Logo">
  
  <h1>Image Lightness Organizer (ILO)</h1>

### A command-line tool for Windows that processes image files and organizes them into folders based on their average lightness value in the CIE L* color space.

</div>

------------------

## 👋 Introduction

**Image Lightness Organizer** is designed to analyze and categorize local image collections. By computing the average lightness ($L^*$) of each image using the scientifically accurate **CIE L* color space**, the application eliminates manual sorting, grouping your media files into structured, percentage-based luminance brackets.

## 👌 Features

- Calculates average image lightness based on human visual perception using the CIE L* color space rather than simple RGB averages.
- Non-destructive processing that safely scans top-level files within the source directory only, ignoring subfolders.
- Creates a dedicated `@Sorted by lightness` output folder and automatically distributes image files into 10 distinct percentage-based subfolders:
  - Light 00%-10%
  - Light 10%-20%
  - Light 20%-30%
  - Light 30%-40%
  - Light 40%-50%
  - Light 50%-60%
  - Light 60%-70%
  - Light 70%-80%
  - Light 80%-90%
  - Light 90%-100%
- Supports the following image formats:
  - JPG / JPEG
  - TIFF / TIF
  - BMP
  - PNG
  - WEBP
  - ICO
  - AVIF

## 🖼️ Screenshots

![screenshot](/Images/screenshot1.png)

![screenshot](/Images/screenshot2.png)

## 🎦 Videos

[Image-Lightness-Organizer BETA DEMO VIDEO](https://github.com/user-attachments/assets/e5148e9b-144c-4e13-852d-3599dc7d7426)

Note: demo video is from an alpha version.

## 📝 Requirements

- Microsoft Windows OS.
- [.NET Runtime 10 (64-Bit)](https://dotnet.microsoft.com/download/dotnet/10.0)

## 🤖 Getting Started

Download the latest release by clicking [here](https://github.com/ElektroStudios/Image-Lightness-Organizer/releases/latest) and start using it.

## 🔄 Change Log

Explore the complete list of changes, bug fixes, and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## 🏆 Credits

This work relies on the following resources: 

 - [SkiaSharp](https://github.com/mono/skiasharp)

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

This Work has no affiliation, approval or endorsement by the author(s) of the third-party libraries used by this Work.

## 💪 Contributing

Your contribution is highly appreciated!. If you have any ideas, suggestions, or encounter issues, feel free to open an issue by clicking [here](https://github.com/ElektroStudios/Image-Lightness-Organizer/issues/new/choose). 

Your input helps make this Work better for everyone. Thank you for your support! 🚀

## 💰 Beyond Contribution 

This work is distributed for educational purposes and without any profit motive. However, if you find value in my efforts and wish to support and motivate my ongoing work, you may consider contributing financially through the following options:

<br></br>
<p align="center"><img src="/Images/github_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Becoming my sponsor on Github:</h3>
<p align="center">You can show me your support by clicking <a href="https://github.com/sponsors/ElektroStudios/">here</a>, <br align="center">contributing any amount you prefer, and unlocking rewards!</br></p>
<br></br>

<p align="center"><img src="/Images/paypal_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Making a Paypal Donation:</h3>
<p align="center">You can donate to me any amount you like via Paypal by clicking <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=E4RQEV6YF5NZY">here</a>.</p>
<br></br>

<p align="center"><img src="/Images/envato_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Purchasing software of mine at Envato's Codecanyon marketplace:</h3>
<p align="center">If you are a .NET developer, you may want to explore '<b>DevCase Class Library for .NET</b>', <br align="center">a huge set of APIs that I have on sale. Check out the product by clicking <a href="https://codecanyon.net/item/elektrokit-class-library-for-net/19260282">here</a></br><br align="center"><i>It also contains all piece of reusable code that you can find across the source code of my open source works.</i></p>
<br></br>

<h2 align="center"><u>Your support means the world to me! Thank you for considering it!</u> 👍</h2>
