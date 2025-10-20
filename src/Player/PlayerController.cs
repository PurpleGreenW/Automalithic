// File: src/Player/PlayerController.cs
using Godot;
using System;

namespace Automalithic.Player
{
    /// <summary>
    /// Simple Player Controller for Godot 4 (C#)
    /// </summary>
    public partial class PlayerController : CharacterBody3D
    {
        [Export] public float MoveSpeed = 6.0f;
        [Export] public float JumpImpulse = 8.0f;
        [Export] public float Gravity = 9.8f;

        private Vector3 _velocity = Vector3.Zero;
        private Camera3D _camera3d;

        public override void _Ready()
        {
            // Assumes the camera node is named "Camera3D" and is a child of this node
            _camera3d = GetNode<Camera3D>("Camera3D");
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _PhysicsProcess(double delta)
        {
            // Gravity
            if (!IsOnFloor())
            {
                _velocity.Y -= Gravity * (float)delta;
            }

            // Jump
            if (Input.IsActionJustPressed("jump") && IsOnFloor())
            {
                _velocity.Y = JumpImpulse;
            }

            // Movement
            Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
            Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            if (direction != Vector3.Zero)
            {
                _velocity.X = direction.X * MoveSpeed;
                _velocity.Z = direction.Z * MoveSpeed;
            }
            else
            {
                _velocity.X = Mathf.MoveToward(_velocity.X, 0, MoveSpeed);
                _velocity.Z = Mathf.MoveToward(_velocity.Z, 0, MoveSpeed);
            }

            Velocity = _velocity;
            MoveAndSlide();
            _velocity = Velocity; // Update _velocity with the result of MoveAndSlide
        }
    }
}