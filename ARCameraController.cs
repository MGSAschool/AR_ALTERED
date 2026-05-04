using Godot;

public class ARCameraController
{
    private const float RadiansToDegrees = 57.29578f;

    public float IntegrateHeading(float currentHeading, double delta, float gyroY)
    {
        float next = currentHeading + gyroY * (float)delta;
        // Wrap to [0, 2π] to prevent unbounded accumulation and float precision loss.
        return ((next % Mathf.Tau) + Mathf.Tau) % Mathf.Tau;
    }

    public void ApplyRotation(Camera3D camera, Vector3 gyro, double delta)
    {
        if (camera == null)
            return;

        camera.RotationDegrees = new Vector3(
            camera.RotationDegrees.X - (gyro.X * (float)delta * RadiansToDegrees),
            camera.RotationDegrees.Y - (gyro.Y * (float)delta * RadiansToDegrees),
            0f
        );
    }
}
