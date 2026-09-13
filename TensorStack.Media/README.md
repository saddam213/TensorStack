# TensorStack.Media
Package for managing Image/Audio and Video from disk

## ImageInput
`ImageInput` extends `ImageTensor` buffered image, provids methods for loading and saving images to disk
- Save


---


## VideoInput
`VideoInput` extends `VideoSequence` bufferd video, provides methods for loading and saving video to disk
- Save

## VideoInputStream
`VideoInputStream` is video backed by file stream allowing loading, data on demand or processing frames one-by-one

## VideoManager
- LoadVideoInfo
- LoadVideoSequence
- WriteVideoStream


---


## AudioInput
`AudioInput` extends `AudioTensor` buffered audio provids methods for loading and saving audio to disk
- Save
- Create


## AudioInputStream
`AudioInputStream` is audio backed by file stream allowing loading of data on demand
- Get
- Move
- Copy
- Create

## AudioManager
- LoadInfo
- LoadTensor
- AddAudio