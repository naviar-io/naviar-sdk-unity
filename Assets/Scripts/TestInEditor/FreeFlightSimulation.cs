using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace naviar.VPSService
{
    public class FreeFlightSimulation : MonoBehaviour
    {
        [SerializeField]
        public VPSLocalisationService VPS;

        private bool isActive = false;

        [Header("Free Flight UI")]
        [SerializeField]
        private Canvas isFreeFlightPrefab;
        private GameObject isFreeFlightObject;

        [Header("Fake localization")]
        [SerializeField]
        private KeyCode simulateLocalizationButton;
        [SerializeField]
        private Transform localizationPose;

        [Header("Localization delay in free flight mode")]
        [SerializeField]
        private float localizationDelay;

        [Header("Moving")]
        [SerializeField]
        private float speed;
        [SerializeField]
        private float accelerationRatio;
        [SerializeField]
        private KeyCode moveForwardButton;
        [SerializeField]
        private KeyCode moveLeftButton;
        [SerializeField]
        private KeyCode moveBackButton;
        [SerializeField]
        private KeyCode moveRightButton;
        [SerializeField]
        private KeyCode AccelerationButtton;

        [Header("Camera rotation")]
        [SerializeField]
        private bool lockCursor;
        [SerializeField]
        private float sensitivity;
        [SerializeField]
        private float maxVerticalAngle = 45;
        private Vector2 turn;

        private Transform player;

        private void Awake()
        {
            ARSessionOrigin arSession = FindObjectOfType<ARSessionOrigin>();
            if (arSession == null)
            {
                Debug.LogError("ARSessionOrigin is not found on scene");
                return;
            }
            player = arSession.camera.transform;
        }

        private void Update()
        {
            if (isActive)
            {
                if (Input.GetKeyDown(simulateLocalizationButton))
                    VPS.MockLocalize(localizationPose.position, localizationPose.rotation);

                RotateCamera();
                MoveCamera();
            }
        }

        private void RotateCamera()
        {
            turn.x += Input.GetAxis("Mouse X") * sensitivity;
            turn.y += Input.GetAxis("Mouse Y") * sensitivity;
            if (turn.y > maxVerticalAngle)
                turn.y = maxVerticalAngle;
            else if (turn.y < -maxVerticalAngle)
                turn.y = -maxVerticalAngle;

            player.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);
        }

        private void MoveCamera()
        {
            if (Input.GetKeyDown(AccelerationButtton))
                speed *= accelerationRatio;

            if (Input.GetKeyUp(AccelerationButtton))
                speed /= accelerationRatio;

            if (Input.GetKey(moveForwardButton))
                player.Translate(Vector3.forward * speed * Time.deltaTime);
            if (Input.GetKey(moveLeftButton))
                player.Translate(Vector3.right * speed * Time.deltaTime * (-1));
            if (Input.GetKey(moveBackButton))
                player.Translate(Vector3.forward * speed * Time.deltaTime * (-1));
            if (Input.GetKey(moveRightButton))
                player.Translate(Vector3.right * speed * Time.deltaTime);
        }

        public void SetFreeFlightMode(bool isActive)
        {
            this.isActive = isActive;
            Cursor.lockState = isActive && lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            ToggleFreeFlightCanvas(isActive);
        }

        public bool IsActive()
        {
            return isActive;
        }

        public float GetLocalizationDelay()
        {
            return localizationDelay;
        }

        private void ToggleFreeFlightCanvas(bool isActive)
        {
            if (isFreeFlightPrefab == null)
                return;

            if (isFreeFlightObject == null)
                isFreeFlightObject = Instantiate(isFreeFlightPrefab).gameObject;

            isFreeFlightObject.SetActive(isActive);
        }
    }
}
