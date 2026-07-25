using UnityEngine;

public sealed class PlayerRailBoundState : PlayerState
{
    private const float InputDeadZone = 0.1f;
    private const float DirectionSampleDistance = 0.1f;

    private float railInput;
    private bool wasKinematic;
    private bool usedGravity;

    public PlayerRailBoundState(Player player, PlayerStateMachine stateMachine, PlayerData playerData)
        : base(player, stateMachine, playerData)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (player.CurrentRail == null)
        {
            Debug.LogError("PlayerRailBoundState requires a bound StraightRail.");
            return;
        }

        wasKinematic = player.RB.isKinematic;
        usedGravity = player.RB.useGravity;

        ClearRigidbodyMotion();
        player.RB.useGravity = false;
        player.RB.isKinematic = true;

        player.SetRailDistance(player.CurrentRail.GetClosestDistance(player.RB.position));
        player.RB.position = player.CurrentRail.GetPosition(player.RailDistance);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        Vector2 rawInput = player.InputHandler.RawMovementInput;
        float inputMagnitude = Mathf.Clamp01(rawInput.magnitude);

        railInput = GetScreenRelativeRailInput(rawInput) * inputMagnitude;

        if (Mathf.Abs(railInput) < InputDeadZone)
        {
            railInput = 0f;
        }
    }

    private float GetScreenRelativeRailInput(Vector2 rawInput)
    {
        if (rawInput.sqrMagnitude <= 0f)
        {
            return 0f;
        }

        Camera movementCamera = Camera.main;
        if (movementCamera == null)
        {
            Vector3 worldInput = player.GetCameraRelativeMovement(rawInput);
            Vector3 railDirection = player.CurrentRail.GetDirection(player.RailDistance);
            return Vector3.Dot(worldInput.normalized, railDirection);
        }

        float previousDistance = Mathf.Max(
            0f,
            player.RailDistance - DirectionSampleDistance);
        float nextDistance = Mathf.Min(
            player.CurrentRail.Length,
            player.RailDistance + DirectionSampleDistance);

        if (Mathf.Approximately(previousDistance, nextDistance))
        {
            return 0f;
        }

        Vector3 previousScreenPoint = movementCamera.WorldToScreenPoint(
            player.CurrentRail.GetPosition(previousDistance));
        Vector3 nextScreenPoint = movementCamera.WorldToScreenPoint(
            player.CurrentRail.GetPosition(nextDistance));
        Vector2 increasingPathScreenDirection = new Vector2(
            nextScreenPoint.x - previousScreenPoint.x,
            nextScreenPoint.y - previousScreenPoint.y);

        if (increasingPathScreenDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Vector2.Dot(
            rawInput.normalized,
            increasingPathScreenDirection.normalized);
    }

    public override void PhysicsUpdate()
    {
        if (player.CurrentRail == null)
        {
            return;
        }

        float nextDistance =
            player.RailDistance +
            railInput * playerData.railMovementVelocity * Time.fixedDeltaTime;

        player.SetRailDistance(nextDistance);
        player.RB.MovePosition(player.CurrentRail.GetPosition(player.RailDistance));
    }

    public override void Exit()
    {

        player.RB.isKinematic = wasKinematic;
        player.RB.useGravity = usedGravity;
        ClearRigidbodyMotion();
        player.ReleaseRail();

        base.Exit();
    }

    private void ClearRigidbodyMotion()
    {
        if (player.RB.isKinematic)
        {
            return;
        }

        player.RB.velocity = Vector3.zero;
        player.RB.angularVelocity = Vector3.zero;
    }
}
