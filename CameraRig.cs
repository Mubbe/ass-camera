using Godot;
using System;

public partial class CameraRig : Node3D
{
    [Export] public Player Player;

    [Export] public SpringArm3D SpringArm;
    [Export] public Camera3D Camera;

    [Export] public RayCast3D CenterRay;
    [Export] public RayCast3D LeftRay;
    [Export] public RayCast3D RightRay;
    [Export] public RayCast3D UpRay;

    [Export] public float FollowDistance = 3.5f;
    [Export] public float FollowHeight = 3.0f;
    [Export] public float FollowSmoothness = 6.0f;

    [Export] public float OrbitSpeed = 120.0f;

    [Export] public float MinPitch = -35.0f;
    [Export] public float MaxPitch = 70.0f;

    [Export] public float AutoAlignSpeed = 3.0f;

    private bool inHintZone = false;
    private Vector3 hintPosition;
    private Vector3 hintRotation;
    private float hintFov = 50.0f;


    private float yaw = 0.0f;
    private float pitch = -15.0f;

    private float currentDistance;

    public override void _Ready()
    {
        InitializeCamera();
        currentDistance = FollowDistance;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Player == null)
            return;

        float dt = (float)delta;

        HandleOrbitInput(dt);
        AutoAlignCamera(dt);
        UpdateCameraRotation();
        UpdateWhiskerRays();
        if (inHintZone)
        {
            UpdateHintCamera(dt);
            return;
        }
       
        FollowPlayer(dt);
    }

    private void InitializeCamera()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        SpringArm.SpringLength = FollowDistance;
    }

    private void HandleOrbitInput(float delta)
    {
        float inputX =
            Input.GetActionStrength("camera_right") -
            Input.GetActionStrength("camera_left");

        float inputY =
            Input.GetActionStrength("camera_down") -
            Input.GetActionStrength("camera_up");

        yaw -= inputX * OrbitSpeed * delta;
        pitch -= inputY * OrbitSpeed * delta;

        pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
    }

    private void AutoAlignCamera(float delta)
    {
        Vector2 moveInput = Input.GetVector(
            "move_left", "move_right",
            "move_forward", "move_back"
        );

        Vector2 cameraInput = Input.GetVector(
            "camera_left", "camera_right",
            "camera_up", "camera_down"
        );

        if (moveInput.Length() < 0.1f || cameraInput.Length() > 0.1f)
            return;

        Vector3 velocity = Player.Get("Velocity").AsVector3();
        Vector3 flat = new Vector3(velocity.X, 0, velocity.Z);

        if (flat.Length() < 0.1f)
            return;

        float targetYaw = Mathf.RadToDeg(Mathf.Atan2(flat.X, flat.Z));

        yaw = Mathf.LerpAngle(yaw, targetYaw, AutoAlignSpeed * delta);
    }

    private void UpdateCameraRotation()
    {
        RotationDegrees = new Vector3(pitch, yaw, 0);
    }

    private Vector3 GetPivot()
    {
        return Player.GlobalPosition + Vector3.Up * FollowHeight;
    }

    private Vector3 GetOrbitDirection()
    {
        float yawRad = Mathf.DegToRad(yaw);
        float pitchRad = Mathf.DegToRad(pitch);

        return new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        ).Normalized();
    }

    private void UpdateWhiskerRays()
    {
        Vector3 pivot = GetPivot();
        Vector3 dir = GetOrbitDirection();

        CenterRay.GlobalPosition = pivot;
        CenterRay.TargetPosition = dir * FollowDistance;
        CenterRay.ForceRaycastUpdate();

        Vector3 right = new Vector3(dir.Z, 0, -dir.X);

        LeftRay.GlobalPosition = pivot;
        LeftRay.TargetPosition = -right * 1.0f;
        LeftRay.ForceRaycastUpdate();

        RightRay.GlobalPosition = pivot;
        RightRay.TargetPosition = right * 1.0f;
        RightRay.ForceRaycastUpdate();

        UpRay.GlobalPosition = pivot;
        UpRay.TargetPosition = Vector3.Up * 1.0f;
        UpRay.ForceRaycastUpdate();
    }

    private void FollowPlayer(float delta)
    {
        Vector3 pivot = GetPivot();
        Vector3 dir = GetOrbitDirection();

        float targetDistance = FollowDistance;

        if (CenterRay.IsColliding())
        {
            float hitDist = pivot.DistanceTo(CenterRay.GetCollisionPoint());
            targetDistance = Mathf.Min(targetDistance, hitDist - 0.2f);

			
        }

        targetDistance = Mathf.Clamp(targetDistance, 1.0f, FollowDistance);

        float t = 1.0f - Mathf.Exp(-FollowSmoothness * delta);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, t);

        Vector3 finalPos = pivot + dir * currentDistance;

        Vector3 right = new Vector3(dir.Z, 0, -dir.X);

        if (LeftRay.IsColliding())
            finalPos += right * 0.3f;

        if (RightRay.IsColliding())
            finalPos -= right * 0.3f;

        if (UpRay.IsColliding())
            finalPos += Vector3.Up * 0.3f;

        GlobalPosition = GlobalPosition.Lerp(finalPos, t);
    }
    private void UpdateHintCamera(float delta)
    {
        float t =1.0f - Mathf.Exp(-FollowSmoothness * delta);
        GlobalPosition =GlobalPosition.Lerp(hintPosition,t);

        RotationDegrees =RotationDegrees.Lerp(hintRotation,t);
        Camera.Fov =Mathf.Lerp(Camera.Fov,hintFov,t);
    }
    public void OnBodyEntered(Node3D body)
    {
        if (body !=Player)
            return;
        inHintZone = true;
        hintPosition = new Vector3( 0, 6, -8);
        hintRotation = new Vector3(-20, 0, 0);
        hintFov = 40.0f;
    }

    public void OnBodyExited(Node3D body)
    {
        if (body != Player)
            return;

        inHintZone = false;
    }
}