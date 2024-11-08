
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CameraManager : MonoBehaviour
{
    private WebCamTexture currentCam; // Changed to currentCam for clarity
    private Texture defaultBackground;

    public RawImage background; // Displays the live camera feed
    public RawImage capturedImage; // Displays the captured photo
    public Button cameraButton; // Button to open the camera
    public Button takePhotoButton; // Button to take a photo
    public Button showPhotoButton; // Button to show the saved photo
    public Button saveToCameraRollButton; // Button to save to camera roll
    public Button switchCameraButton; // Button to switch between front and back cameras

    private string albumName = "Warini"; // Album name in the gallery
    private string fileName = "Warini.png"; // File name for the saved image

    private bool isUsingFrontCamera = true; // Flag to track which camera is in use

    void Start()
    {
        cameraButton.onClick.AddListener(OpenCamera);
        takePhotoButton.onClick.AddListener(TakePhoto);
        showPhotoButton.onClick.AddListener(ShowPhoto);
        saveToCameraRollButton.onClick.AddListener(SaveToCameraRoll); // Hook up the button
        switchCameraButton.onClick.AddListener(SwitchCamera); // Hook up the switch camera button
    }

    public void OpenCamera()
    {
        defaultBackground = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("No camera detected");
            return;
        }

        // Find the appropriate camera based on the isUsingFrontCamera flag
        for (int i = 0; i < devices.Length; i++)
        {
            if (isUsingFrontCamera && devices[i].isFrontFacing)
            {
                currentCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                break; // Exit loop once we find the front camera
            }
            else if (!isUsingFrontCamera && !devices[i].isFrontFacing)
            {
                currentCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                break; // Exit loop once we find the back camera
            }
        }

        if (currentCam == null)
        {
            Debug.Log("Unable to find the camera");
            return;
        }

        currentCam.Play();
        background.texture = currentCam;

        // Adjust orientation based on device's camera rotation
        background.rectTransform.localEulerAngles = new Vector3(0, 0, -currentCam.videoRotationAngle);
        background.rectTransform.localScale = new Vector3(currentCam.videoVerticallyMirrored ? -1 : 1, 1, 1);
    }

    public void SwitchCamera()
    {
        isUsingFrontCamera = !isUsingFrontCamera; // Toggle the camera flag
        if (currentCam != null && currentCam.isPlaying)
        {
            currentCam.Stop(); // Stop the current camera feed
        }
        OpenCamera(); // Open the selected camera
    }

    public void TakePhoto()
    {
        if (currentCam != null && currentCam.isPlaying)
        {
            Texture2D photo = new Texture2D(currentCam.width, currentCam.height);
            photo.SetPixels(currentCam.GetPixels());
            photo.Apply();

            // Display the captured photo in capturedImage RawImage without stopping the camera feed
            capturedImage.texture = photo;

            // Save the captured photo to storage
            SavePhoto(photo, albumName, fileName);
        }
    }

    private void SavePhoto(Texture2D photo, string albumName, string fileName)
    {
        byte[] bytes = photo.EncodeToPNG();
        string directoryPath = Path.Combine(Application.persistentDataPath, albumName);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log("Created directory: " + directoryPath);
        }

        string filePath = Path.Combine(directoryPath, fileName);

        try
        {
            File.WriteAllBytes(filePath, bytes);
            Debug.Log("Saved photo to: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save photo: " + e.Message);
        }
    }

    public void ShowPhoto()
    {
        string filePath = Path.Combine(Application.persistentDataPath, albumName, fileName);

        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D photo = new Texture2D(2, 2); // Create a new Texture2D with an initial small size
            photo.LoadImage(fileData); // Load the image data into the texture
            photo.Apply();

            // Set the loaded photo as the capturedImage texture
            capturedImage.texture = photo;
            Debug.Log("Displayed saved photo from: " + filePath);
        }
        else
        {
            Debug.Log("No photo found to display.");
        }
    }

    public void SaveToCameraRoll()
    {
        // Ensure that the captured image is set
        if (capturedImage.texture is Texture2D photo)
        {
            // Encode the texture to PNG
            byte[] bytes = photo.EncodeToPNG();

            // Get the path where the image will be saved
            string path = Path.Combine(Application.persistentDataPath, albumName, fileName);

            // Save the image to the persistent path
            File.WriteAllBytes(path, bytes);

            // Save to the camera roll using Native Gallery
            NativeGallery.SaveImageToGallery(photo, albumName, fileName, (success, error) =>
            {
                if (success)
                {
                    Debug.Log("Saved photo to camera roll.");
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError("Error saving photo: " + error);
                }
            });
        }
        else
        {
            Debug.Log("No photo available to save.");
        }
    }
}