# Y8CgCoA4 for Unity
Y8CgCoA4 is a is a lossy texture compression format for iOS/Android
inspired by [ChromaPack](https://github.com/keijiro/ChromaPack).
The differencec between ChromaPack and Y8CgCoA4 are:
* Encoded color space is [YCgCo](https://en.wikipedia.org/wiki/YCgCo), which can be decoded effectively.
* Alpha is supported.
* Cg Co and Alpha components are stored in a 4 bpp block compression format (ETC1 or PVRTC).
