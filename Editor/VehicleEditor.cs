#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DemoVehicleSystem
{
    [CustomEditor(typeof(VehicleMovement))]
    public class VehicleEditor : Editor
    {
        private enum DriveType
        {
            front,
            rear,
            both
        }

        private DriveType driveType = DriveType.rear;

        private bool editingVehicle = false;
        private bool editingWheels = false;
        private bool generatingEssentials = false;
        private bool overrideWheelValues = false;

        private static string nameBack = "B";
        private static string nameFront = "F";
        private static string bodyName = "Car";

        private GUIStyle buttonStyle;
        private VehicleMovement vehicle;

        private GameObject bodyMesh = null;

        float radius = -1;
        float centerY = -1;
        float suspensionDist = -1;
        float mass = -1;

        private float spring = 90000;
        private float damper = 9000;

        private string GetDrive()
        {
            return (driveType == DriveType.both) ? "↨ All wheel drive" : 
                (driveType == DriveType.rear ? "↓ Rear wheel drive" :
                "↑ Front wheel drive");
        }


        private void EditingWheels()
        {
            var wheels = vehicle.GetWheels();

            if (wheels == null || wheels.Length == 0) return;

            if (radius == -1)
            {
                radius = wheels[0].collider.radius;
                mass = wheels[0].collider.mass;
                suspensionDist = wheels[0].collider.suspensionDistance;
                centerY = wheels[0].collider.center.y;
                spring = wheels[0].collider.suspensionSpring.spring;
                damper = wheels[0].collider.suspensionSpring.damper;
            }
            else
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(radius));
                radius = EditorGUILayout.Slider(radius, 0, 10.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(suspensionDist));
                suspensionDist = EditorGUILayout.Slider(suspensionDist, -1.0f, 1.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(centerY));
                centerY = EditorGUILayout.Slider(centerY, -1.0f, 1.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(mass));
                mass = EditorGUILayout.FloatField(mass);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(spring));
                spring = EditorGUILayout.FloatField(spring);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(nameof(damper));
                damper = EditorGUILayout.FloatField(damper);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i].collider.gameObject.name.Contains(nameBack))
                {
                    if (driveType == DriveType.rear || driveType == DriveType.both) wheels[i].isDrive = true;
                }
                else
                {
                    if (driveType == DriveType.front || driveType == DriveType.both) wheels[i].isDrive = true;
                    wheels[i].isTurn = true;
                }

                wheels[i].collider.radius = radius;
                wheels[i].collider.mass = mass;
                wheels[i].collider.suspensionDistance = suspensionDist;
                wheels[i].collider.center = new Vector3(wheels[i].collider.center.x, centerY, wheels[i].collider.center.z);

                var sus = wheels[i].collider.suspensionSpring;
                sus.spring = spring;
                sus.damper = damper;

                wheels[i].collider.suspensionSpring = sus;
            }

            vehicle.SetWheels(wheels);

            if (GUILayout.Button("Back")) editingWheels = false;
        }

        private void GenerateEssentials()
        {
            var vehicle = (VehicleMovement)target;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Body naming convention");
            bodyName = GUILayout.TextField(bodyName);
            GUILayout.EndHorizontal();

            if (bodyMesh == null || bodyMesh.name.Contains(bodyName) == false)
            {
                foreach (var item in vehicle.gameObject.GetComponentsInChildren<MeshRenderer>())
                {
                    if (item.gameObject.name.Contains(bodyName))
                    {
                        bodyMesh = item.gameObject;
                        break;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Generate essentials"))
                {
                    var rb = vehicle.gameObject.AddComponent<Rigidbody>();
                    rb.drag = 0.05f;
                    rb.angularDrag = 0.05f;
                    rb.mass = 1500;

                    bodyMesh.AddComponent<BoxCollider>();
                    GenerateWheelColliders(vehicle.GetComponentsInChildren<MeshRenderer>());

                    EditorUtility.DisplayDialog("WARNING!", "Please be sure to manually validate the state of the vehicle colliders by hand.", "Ok");
                }
            }
        }

        private void GenerateWheelColliders(MeshRenderer[] meshes)
        {
            var transform = vehicle.transform;

            // Create the target gameobject
            var parent = new GameObject("Wheel colliders").transform;
            parent.SetParent(transform, false);
            parent.localPosition = Vector3.zero;

            // Grab all the wheel objects
            var wheels = new List<GameObject>();
            foreach (var item in meshes) if (item.name.Contains(nameBack) || item.name.Contains(nameFront)) wheels.Add(item.gameObject);

            // create an empty
            var empty = new GameObject("Empty");
            empty.AddComponent<WheelCollider>();

            foreach (var item in wheels)
            {
                var obj = Instantiate(empty, item.transform.position, item.transform.rotation, parent);
                obj.name = item.name;
            }

            // clean up empty
            DestroyImmediate(empty);
        }

        private void SetupVehicleWheels() 
        {
            if (GUILayout.Button("Setup wheels"))
            {
                var wheelColliders = vehicle.GetComponentsInChildren<WheelCollider>();
                var wheelMesh = vehicle.GetComponentsInChildren<MeshRenderer>();
                var wheels = new List<VehicleMovement.Wheel>();
                var wheel = new VehicleMovement.Wheel();
                wheel.isBreak = true;

                if (wheelColliders == null || wheelColliders.Length == 0)
                {
                    bool value = EditorUtility.DisplayDialog("ERROR!", "Could not find WheelColliders! Please ensure that the Vehicle component is a parent of all  the wheels components.", "Generate them for me.", "Ok, I'll do it myself.");

                    if (value)
                    {
                        // Attempt to automatically generate the wheel colliders

                        GenerateWheelColliders(wheelMesh);
                    }
                    else
                    {
                        Debug.LogError("Could not find WheelColliders! Please ensure that the Vehicle component is a parent of all  the wheels components.");
                    }

                    return;
                }

                foreach (var wheelCol in wheelColliders)
                {
                    var wheelName = wheelCol.gameObject.name;

                    if (wheelName.Contains(nameBack))
                    {
                        if (driveType == DriveType.rear || driveType == DriveType.both) wheel.isDrive = true;
                    }
                    else
                    {
                        if (driveType == DriveType.front || driveType == DriveType.both) wheel.isDrive = true;
                        wheel.isTurn = true;
                    }

                    wheel.collider = wheelCol;

                    if (overrideWheelValues)
                    {
                        var sus = wheelCol.suspensionSpring;
                        sus.spring = spring;
                        sus.damper = damper;

                        wheelCol.suspensionSpring = sus;
                    }

                    foreach (var item in wheelMesh)
                    {
                        if (wheelName.Contains(item.gameObject.name) || item.gameObject.name.Contains(wheelName))
                        {
                            wheel.transform = item.transform;
                            break;
                        }
                    }

                    wheels.Add(wheel);
                }

                vehicle.TrySetEssentials();
                vehicle.SetWheels(wheels.ToArray());
            }
        }

        private void EditingVehicle()
        {
            if (generatingEssentials)
            {
                GenerateEssentials();

                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Back wheel naming convention");
            nameBack = GUILayout.TextField(nameBack);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Front wheel naming convention");
            nameFront = GUILayout.TextField(nameFront);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button($"{GetDrive()}", buttonStyle))
            {
                switch (driveType)
                {
                    case DriveType.front:
                        driveType = DriveType.rear;
                        break;
                    case DriveType.rear:
                        driveType = DriveType.both;
                        break;
                    case DriveType.both:
                        driveType = DriveType.front;
                        break;
                }
            }

            if (GUILayout.Button(overrideWheelValues ? "○ Override wheel collider values" : "• Use user defined wheel collider values", buttonStyle)) overrideWheelValues = !overrideWheelValues;

            GUILayout.Space(5);

            if (vehicle.GetWheels() != null && vehicle.GetWheels().Length > 0)
            {
                if (editingWheels == false && GUILayout.Button("Edit wheels"))
                {
                    editingWheels = true;
                }
                else if (editingWheels)
                {
                    EditingWheels();
                    return;
                }

                if (GUILayout.Button("Apply drive type"))
                {
                    var wheels = vehicle.GetWheels();

                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if (wheels[i].collider.gameObject.name.Contains(nameBack))
                        {
                            if (driveType == DriveType.rear || driveType == DriveType.both) wheels[i].isDrive = true;
                        }
                        else
                        {
                            if (driveType == DriveType.front || driveType == DriveType.both) wheels[i].isDrive = true;
                            wheels[i].isTurn = true;
                        }
                    }

                    vehicle.SetWheels(wheels);
                    editingWheels = false;
                    editingVehicle = false;
                }
            }
            else
            {
                SetupVehicleWheels();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Back")) editingVehicle = false;
        }

        public override void OnInspectorGUI()
        {
            if (editingVehicle == false)
            {
                base.OnInspectorGUI();

                GUILayout.Space(20);

                if (GUILayout.Button("Setup/Edit vehicle"))
                {
                    vehicle = (VehicleMovement)target;
                    editingVehicle = true;
                    generatingEssentials = ((VehicleMovement)target).GetComponentInChildren<Rigidbody>() == null;

                    if (buttonStyle == null)
                    {
                        buttonStyle = new GUIStyle(GUI.skin.box);
                        buttonStyle.alignment = TextAnchor.MiddleLeft;
                    }
                }
            }
            else
            {
                if (editingVehicle) EditingVehicle();
            }
        }
    }
}

#endif