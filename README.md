BUILDLet JBIG Image Viewer
==========================

Introduction
------------

This project provides the UWP (*Universal Windows Platform*) viewer app especially for **JBIG** (*Joint Bi-level Image experts Group*) image.

![JBIG Image Viewer](./README/HomePage.png "JBIG Image Viewer")

Getting Started
---------------

1. Download the certification file [BUILDLet.cer](./Certificate/BUILDLet.cer) to your computer.
2. Import the downloaded certification file (*BUILDLet.cer*) into your *Trusted Root Certification Authorities* of *Local Machine*, if you can trust us.
3. Download the installer package for your platform ([x86](./BUILDLet.JbigImageViewer/AppPackages/BUILDLet.JbigImageViewer_1.0.0.0_x86_Test/BUILDLet.JbigImageViewer_1.0.0.0_x86.msix), [x64](./BUILDLet.JbigImageViewer/AppPackages/BUILDLet.JbigImageViewer_1.0.0.0_x64_Test/BUILDLet.JbigImageViewer_1.0.0.0_x64.msix), [ARM](./BUILDLet.JbigImageViewer/AppPackages/BUILDLet.JbigImageViewer_1.0.0.0_ARM_Test/BUILDLet.JbigImageViewer_1.0.0.0_ARM.msix) or [ARM64](./BUILDLet.JbigImageViewer/AppPackages/BUILDLet.JbigImageViewer_1.0.0.0_ARM64_Test/BUILDLet.JbigImageViewer_1.0.0.0_ARM64.msix)) on your computer, and execute it.

Trouble Shooting
----------------

- **Phenomenon:**
  If our certification file ([BUILDLet.cer](./Certificate/BUILDLet.cer)) has not correctly imported into your *Trusted Root Certification Authorities* of *Local Machine*, the "**Install**" button of the installer package (*msix*) is not enabled.

- **Solution:**
  Download our certification file [BUILDLet.cer](./Certificate/BUILDLet.cer) to your computer, and Import it to your *Trusted Root Certification Authorities* of *Local Machine* if you can trust us, before you launch the installer package.

![Trusted Installer Package](./README/msix_trusted.png "Trusted Installer Package")

Build and Test
--------------

- This project (*Visual Studio Solution*) is built and tested on Visual Studio.
- We did not tested our App on *ARM* Platform (including *ARM64*).

License
-------

This project is licensed under the [MIT](https://opensource.org/licenses/MIT) License.

Also, this project is cantaining **[BUILDLet.Imaging.Jbig](https://github.com/buildlet/BUILDLet.Imaging.Jbig)**, which is including **[JBIT-KIT](https://www.cl.cam.ac.uk/~mgk25/jbigkit/)** (*jbig1.dll*) and **[NetPbm for Windows](http://gnuwin32.sourceforge.net/packages/netpbm.htm)** (*jbigtopnm.exe*, *ppmtobmp.exe* and *libnetpbm10.dll*).
Regarding **JBIG-KIT** and **NetPbm for Windows**, please refer to *README* of **BUILDLet.Imaging.Jbig**.
