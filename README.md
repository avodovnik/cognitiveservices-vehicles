# cognitiveservices-vehicles
A Cognitive Services Application for managing visitors in vehicle lanes

## Azure Functions

The code for Azure Functions used in this system is in the "Functions" directory.

We use a .NET Azure Function (a custom-written web service) to run detection and OCR on number plates, in a process called NPR (Number Plate Recognition).

[Find out more about Azure Functions](https://azure.microsoft.com/en-gb/services/functions/?&OCID=AID719823_SEM_0DwINtxM&dclid=CMCWgYfO4twCFUtj0wodcTkJSQ)

In the 'Functions/NPR' directory, OCRResponse.cs is merely a response object generated at [json2csharp](http://json2csharp.com/). This helps deal with API responses in NPRFunction.cs - in which we call to 2 Microsoft Cognitive Services: [Custom Vision](https://customvision.ai) (specifically an object detection model), followed by the OCR endpoint of [Computer Vision](https://azure.microsoft.com/en-gb/services/cognitive-services/computer-vision/).

This process runs in 3 steps:

1. Given an input image of the front of a vehicle, detect the number/license plate with Custom Vision.
2. Crop the input image to only the number plate using the boundings provided by the detection model.
3. Run this cropped image through the OCR service to recognize the string of characters, then concatenate and return this as the output of the function.