using UnityEngine;

public class TPoseController : MonoBehaviour
{
    [Header("T-Pose Settings")]
    [Tooltip("Enable to set T-Pose on Start")]
    public bool setTPoseOnStart = true;

    [Tooltip("Angle for the arms (90 = horizontal, parallel to horizon)")]
    [Range(0f, 180f)]
    public float armAngle = 90f;

    private Transform leftUpperArm;
    private Transform rightUpperArm;

    void Start()
    {
        FindArmBones();

        if (setTPoseOnStart)
        {
            SetTPose();
        }
    }

    private void FindArmBones()
    {
        // Find the arm bones in the hierarchy
        Transform[] allChildren = GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.name == "Left_UpperArm")
                leftUpperArm = child;
            else if (child.name == "Right_UpperArm")
                rightUpperArm = child;
        }

        if (leftUpperArm == null || rightUpperArm == null)
        {
            Debug.LogWarning("Could not find arm bones. Make sure this script is attached to Robot Kyle.");
        }
    }

    [ContextMenu("Set T-Pose")]
    public void SetTPose()
    {
        if (leftUpperArm != null && rightUpperArm != null)
        {
            // Set arms to be horizontal (parallel to the horizon)
            // Adjust the rotation based on your character's rig orientation
            leftUpperArm.localRotation = Quaternion.Euler(0, 0, -armAngle);
            rightUpperArm.localRotation = Quaternion.Euler(0, 0, armAngle);

            Debug.Log("T-Pose applied to Robot Kyle!");
        }
        else
        {
            Debug.LogError("Arm bones not found! Make sure script is on Robot Kyle.");
        }
    }

    [ContextMenu("Reset Pose")]
    public void ResetPose()
    {
        if (leftUpperArm != null && rightUpperArm != null)
        {
            leftUpperArm.localRotation = Quaternion.identity;
            rightUpperArm.localRotation = Quaternion.identity;

            Debug.Log("Pose reset!");
        }
    }
}
