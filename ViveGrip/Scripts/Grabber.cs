﻿using UnityEngine;
using System.Collections;

public class Grabber : MonoBehaviour {
  public float grabRadius = 0.5f;
  public Shader outline;
  public Color outlineColor;
  public bool grabberSphereVisible = false;
  public ulong gripInput = SteamVR_Controller.ButtonMask.Grip;
  private Shader oldShader;
  // TODO: set outline in script with Shader.Find
  private GameObject currentObject;
  private GrabberSphere grabberSphere;
  private ConfigurableJoint grabberJoint;
  private GameObject jointObject;
  private bool anchored = false;
  private SteamVR_Controller.Device device;

  private Vector3 defaultAnchor = new Vector3(0, 0, 0.5f);

  void Awake() {
    //device = GetDevice();
  }

  void Start() {
    grabberJoint = InstantiateJoint();
    GameObject grabberObject = InstantiateGrabberObjectOn(grabberJoint);
    grabberSphere = grabberObject.AddComponent<GrabberSphere>();
    grabberSphere.radius = grabRadius;
	}

  void Update() {
    device = GetDevice();

    GameObject touchedObject = grabberSphere.ClosestObject();
    if (!SomethingHeldBy(grabberJoint) && device.GetTouchDown(gripInput)) {
      Connect(grabberJoint, touchedObject.GetComponent<Rigidbody>());
    }

    UpdateHighlighting(touchedObject);

    if (SomethingHeldBy(grabberJoint) && device.GetTouchUp(gripInput)) {
      Disconnect(grabberJoint);
    }

    if (SomethingHeldBy(grabberJoint)) {
      float grabDistance = Vector3.Distance(WorldAnchorPositionFor(grabberJoint), grabberJoint.connectedBody.transform.position);
      anchored = anchored || PulledToMiddle(grabDistance);
      if (anchored && grabDistance > grabRadius) { // TODO: togglable please
        Debug.Log(grabDistance + " > " + grabRadius);
        Debug.Log(touchedObject + " // " + grabberJoint.connectedBody);
        Disconnect(grabberJoint);
      }
    }
  }

  bool PulledToMiddle(float distance) {
    return distance < grabRadius;
  }

  SteamVR_Controller.Device GetDevice() {
    // TODO: assumes that parent is the controller object
    SteamVR_TrackedObject trackedObject = transform.parent.GetComponent<SteamVR_TrackedObject>();
    return SteamVR_Controller.Input((int)trackedObject.index);
  }

  void Connect(ConfigurableJoint joint, Rigidbody desiredObject) {
    joint.connectedBody = desiredObject;
    joint.connectedBody.useGravity = false;


    // Vector3 debugVector = LocalAnchorPositionFor(joint);
    // debugVector.Normalize();
    // float debugScalar = WorldAnchorPositionFor(joint).z - joint.connectedBody.transform.position.z;
    // Vector3 localDifference = -debugVector * debugScalar;
    // joint.targetPosition = localDifference;
    //---
    // Vector3 debugScalars = WorldAnchorPositionFor(joint) - joint.connectedBody.transform.position;
    // debugScalars.Normalize();
    // // Vector3 localDifference = -Vector3.Scale(debugVector, debugScalars);
    // joint.targetPosition = debugScalars;//localDifference;
    // ---
    // joint.targetPosition = Vector3.zero;//WorldAnchorPositionFor(joint) - joint.connectedBody.transform.position;
    // Vector3 debugScalars = (LocalAnchorPositionFor(joint) - transform.position) - joint.connectedBody.transform.position;


    Vector3 moveMe = joint.connectedAnchor - desiredObject.transform.position;
    //joint.anchor += WorldAnchorPositionFor(joint) - joint.connectedBody.transform.position;
    //LocalAnchorPositionFor(joint) - transform.TransformVector(joint.connectedBody.transform.position);
    // Debug.Log(jointObject.transform.position + " // " + transform.TransformVector(moveMe));
    //Debug.Log(transform.position + " + " + moveMe);
    Debug.DrawLine(joint.connectedAnchor, desiredObject.transform.position, Color.red, 20, false);
    Vector3 realAnchor = transform.position + transform.TransformVector(defaultAnchor);
    Debug.DrawLine(realAnchor, desiredObject.transform.position, Color.yellow, 20, false);
    joint.connectedAnchor += realAnchor - desiredObject.transform.position;
    //jointObject.transform.position += moveMe;
    //Debug.Log(jointObject.transform.position);
    Debug.Log("Connected to " + desiredObject.gameObject.name);
    // Debug.Log(LocalAnchorPositionFor(joint) + " l//w " + WorldAnchorPositionFor(joint) + " <-- " + transform.TransformVector(joint.connectedBody.transform.position) + " l//w " + joint.connectedBody.transform.position);
    // Debug.Log(joint.targetPosition);
  }

  void Disconnect(ConfigurableJoint joint) {
    Debug.Log(joint);
    Debug.Log(joint.connectedBody);
    joint.connectedBody.useGravity = true;
    joint.connectedBody = null;
    joint.targetPosition = Vector3.zero; // TODO: needed?
    joint.anchor = defaultAnchor;
    anchored = false;
    //jointObject.transform.position = transform.position;
    Debug.Log("Disconnected");
  }

  Vector3 WorldAnchorPositionFor(ConfigurableJoint joint) {
    return transform.TransformVector(LocalAnchorPositionFor(joint));
  }

  Vector3 LocalAnchorPositionFor(ConfigurableJoint joint) {
    return Vector3.Scale(joint.anchor, transform.GetComponent<Renderer>().bounds.size);
  }

  ConfigurableJoint InstantiateJoint() {
    // jointObject = new GameObject("Joint Object");
    // jointObject.transform.parent = transform;
    // jointObject.transform.localPosition = Vector3.zero;
    // jointObject.transform.localScale = Vector3.one;
    // ConfigurableJoint joint = jointObject.AddComponent<ConfigurableJoint>();

    ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
    GetComponent<Rigidbody>().useGravity = false;
    GetComponent<Rigidbody>().isKinematic = true;
    // jointObject.GetComponent<Rigidbody>().useGravity = false;
    // jointObject.GetComponent<Rigidbody>().isKinematic = true;
    joint.xMotion = ConfigurableJointMotion.Limited;
    joint.yMotion = ConfigurableJointMotion.Limited;
    joint.zMotion = ConfigurableJointMotion.Limited;
    joint.angularXMotion = ConfigurableJointMotion.Locked;
    joint.angularYMotion = ConfigurableJointMotion.Locked;
    joint.angularZMotion = ConfigurableJointMotion.Locked;

    joint.anchor = defaultAnchor;

    SoftJointLimit jointLimit = joint.linearLimit;
    jointLimit.limit = 10;
    joint.linearLimit = jointLimit;

    float quiteStrong = 50000f; // TODO: the higher the better but Mathf.Infinity breaks it...
    // TODO: scale this to mass? 500*mass
    float somewhatSignificant = 1f;
    JointDrive jointDrive = joint.xDrive;
    jointDrive.positionSpring = quiteStrong;
    jointDrive.positionDamper = somewhatSignificant;
    joint.xDrive = jointDrive;
    jointDrive = joint.yDrive;
    jointDrive.positionSpring = quiteStrong;
    jointDrive.positionDamper = somewhatSignificant;
    joint.yDrive = jointDrive;
    jointDrive = joint.zDrive;
    jointDrive.positionSpring = quiteStrong;
    jointDrive.positionDamper = somewhatSignificant;
    joint.zDrive = jointDrive;

    return joint;
  }

  GameObject InstantiateGrabberObjectOn(ConfigurableJoint joint) {
    GameObject grabberObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    if (!grabberSphereVisible) {
      grabberObject.GetComponent<Renderer>().enabled = false;
    }
    grabberObject.transform.localScale = new Vector3(grabRadius, grabRadius, grabRadius);
    grabberObject.transform.SetParent(transform.parent);
    grabberObject.transform.localPosition = LocalAnchorPositionFor(joint);
    grabberObject.name = "Grabber Sphere";
    return grabberObject;
  }

  void UpdateHighlighting(GameObject touchedObject) {
    if (touchedObject != currentObject) {
      if (currentObject != null) {
        currentObject.GetComponent<Renderer>().material.shader = oldShader;
      }
      currentObject = touchedObject;
      if (touchedObject != null) {
        oldShader = currentObject.GetComponent<Renderer>().material.shader;
        currentObject.GetComponent<Renderer>().material.shader = outline;
        currentObject.GetComponent<Renderer>().material.SetFloat("_Outline", 0.0005f);
        currentObject.GetComponent<Renderer>().material.SetColor("_OutlineColor", outlineColor);
      }
    }
  }

  bool SomethingHeldBy(ConfigurableJoint joint) {
    return joint.connectedBody != null;
  }
}


  // static void SetTargetRotationInternal (ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation)
  // {
  //   // Calculate the rotation expressed by the joint's axis and secondary axis
  //   var right = joint.axis;
  //   var forward = Vector3.Cross (joint.axis, joint.secondaryAxis).normalized;
  //   var up = Vector3.Cross (forward, right).normalized;
  //   Quaternion worldToJointSpace = Quaternion.LookRotation (forward, up);

  //   // Transform into world space
  //   Quaternion resultRotation = Quaternion.Inverse (worldToJointSpace);

  //   // Counter-rotate and apply the new local rotation.
  //   // Joint space is the inverse of world space, so we need to invert our value
  //   // world: resultRotation *= startRotation * Quaternion.Inverse (targetRotation);
  //   resultRotation *= Quaternion.Inverse (targetRotation) * startRotation;

  //   // Transform back into joint space
  //   resultRotation *= worldToJointSpace;

  //   // Set target rotation to our newly calculated rotation
  //   joint.targetRotation = resultRotation;
  // }
