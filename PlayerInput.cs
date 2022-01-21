using UnityEngine;
using Cinemachine;

namespace DemoVehicleSystem
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private VehicleMovement vehicle;

        [SerializeField] private float rotateSpeed = 5.0f;

        private void UpdateVehicleInput()
        {
            if (vehicle == null) return;

            vehicle.input = Vector3.zero;
            // Throttle
            vehicle.input.z = Input.GetAxis("Vertical");
            // Steer
            vehicle.input.x = Input.GetAxis("Horizontal");
            // Braking
            vehicle.input.y = Input.GetButton("Jump") ? 1 : 0;
        }

        void Update()
        {
            UpdateVehicleInput();

            transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * rotateSpeed);

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (vehicle != null)
                {
                    vehicle = null;
                    transform.SetParent(null);
                }
                else
                {
                    vehicle = FindObjectOfType<VehicleMovement>();
                    transform.SetParent(vehicle.transform);
                    transform.position = vehicle.transform.position;
                }
            }
        }
    }
}