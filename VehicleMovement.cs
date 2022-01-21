using UnityEngine;

namespace DemoVehicleSystem
{
    public class VehicleMovement : MonoBehaviour
    {
        internal Vector3 input;

        [System.Serializable]
        public struct Wheel
        {
            public WheelCollider collider;
            public Transform transform;

            public bool isDrive;
            public bool isTurn;
            public bool isBreak;
        }

        [Header("Setup")]
        [SerializeField] private Wheel[] wheels;
        [SerializeField] private BoxCollider carCollider;

        [Header("Handling")]
        [SerializeField] private float strengthCoefficient = 1500.0f;
        [SerializeField] private float maxTurn = 60.0f;
        [Range(0.1f, 3.0f)]
        [SerializeField] private float brakeTorque = 3.0f;

        [SerializeField] private Rigidbody rigidBody = null;
        private float isGrounded = 0.0f;
        [SerializeField] private float maxAirTime = 0.25f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            TrySetEssentials();
        }

        public void TrySetEssentials()
        {
            if (rigidBody == null) rigidBody = GetComponentInChildren<Rigidbody>();
            if (carCollider == null) carCollider = GetComponentInChildren<BoxCollider>();
        }
        public void SetWheels(Wheel[] wheels) => this.wheels = wheels;
        public Wheel[] GetWheels() => wheels;
#endif

        private void UpdateWheelVisual(Wheel wheel)
        {
            Vector3 pos;
            Quaternion rot;
            wheel.collider.GetWorldPose(out pos, out rot);
            wheel.transform.rotation = rot;
        }

        private void Update()
        {
            if (isGrounded >= maxAirTime)
            {
                var rot = Quaternion.FromToRotation(transform.up, Vector3.up);
                rigidBody.AddTorque(new Vector3(rot.x, rot.y, rot.z) * strengthCoefficient);
            }

            isGrounded -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            foreach (var wheel in wheels)
            {
                if (wheel.isDrive) wheel.collider.motorTorque = input.z * strengthCoefficient;
                if (wheel.isTurn)
                {
                    // figure out the correct turn angle for the front wheels
                    float angle_0 = input.x * maxTurn;
                    if (Mathf.Abs(input.x * maxTurn) > Mathf.Epsilon)
                    {
                        float radius = carCollider.size.z / Mathf.Tan(input.x * maxTurn * Mathf.Deg2Rad);
                        float radius_0 = radius + carCollider.size.x * (wheel.transform.localPosition.x * 0.5f);

                        angle_0 = Mathf.Atan(carCollider.size.z / radius_0) * Mathf.Rad2Deg;
                    }

                    wheel.collider.steerAngle = angle_0;
                }

                if (wheel.isBreak) wheel.collider.brakeTorque = (strengthCoefficient * brakeTorque) * input.y;

                UpdateWheelVisual(wheel);

                if (wheel.collider.isGrounded == false) isGrounded += Time.deltaTime;
            }
        }
    }
}