using Godot;
using Automalithic.World;

namespace Automalithic.Player
{
    /// <summary>
    /// First-Person-Controller für den Spieler im God-Perspective-Modus.
    /// Ermöglicht freie Bewegung durch die Voxel-Welt mit Maus und Tastatur.
    /// </summary>
    public partial class PlayerController : CharacterBody3D
    {
        // ==================== Export-Variablen ====================
        /// <summary>
        /// Bewegungsgeschwindigkeit in Metern pro Sekunde
        /// </summary>
        [Export] public float MoveSpeed = 10.0f;
        
        /// <summary>
        /// Sprint-Multiplikator für schnellere Bewegung
        /// </summary>
        [Export] public float SprintMultiplier = 2.0f;
        
        /// <summary>
        /// Maus-Sensitivität für Kamerasteuerung
        /// </summary>
        [Export] public float MouseSensitivity = 0.002f;
        
        /// <summary>
        /// Maximaler Kamera-Neigungswinkel in Grad
        /// </summary>
        [Export] public float MaxPitchAngle = 89.0f;

        // ==================== Private Felder ====================
        /// <summary>
        /// Referenz zur Kamera-Node
        /// </summary>
        private Camera3D _camera;
        
        /// <summary>
        /// Aktueller Pitch-Winkel der Kamera
        /// </summary>
        private float _cameraPitch;
        
        /// <summary>
        /// Referenz zur VoxelWorld für Chunk-Updates
        /// </summary>
        private VoxelWorld _voxelWorld;
        
        /// <summary>
        /// Flag ob Maus gefangen ist
        /// </summary>
        private bool _mouseCapture = true;

        // ==================== Godot Lifecycle ====================
        /// <summary>
        /// Initialisierung beim Laden der Node
        /// </summary>
        public override void _Ready()
        {
            // Kamera-Node suchen
            _camera = GetNode<Camera3D>("Camera3D");
            if (_camera == null)
            {
                GD.PrintErr("PlayerController: Camera3D-Node nicht gefunden!");
            }
            
            // VoxelWorld im Parent suchen
            _voxelWorld = GetNode<VoxelWorld>("/root/Main/VoxelWorld");
            if (_voxelWorld == null)
            {
                GD.PrintErr("PlayerController: VoxelWorld nicht gefunden!");
            }
            
            // Maus einfangen für FPS-Steuerung
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        /// <summary>
        /// Input-Events verarbeiten (Mausbewegung, Tasten)
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            // Mausbewegung für Kamera
            if (@event is InputEventMouseMotion mouseMotion && _mouseCapture)
            {
                // Horizontal: Player rotieren
                RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                
                // Vertikal: Kamera neigen
                _cameraPitch -= mouseMotion.Relative.Y * MouseSensitivity;
                _cameraPitch = Mathf.Clamp(_cameraPitch, 
                    -Mathf.DegToRad(MaxPitchAngle), 
                    Mathf.DegToRad(MaxPitchAngle));
                
                if (_camera != null)
                {
                    _camera.Rotation = new Vector3(_cameraPitch, 0, 0);
                }
            }
            
            // ESC zum Maus freigeben
            if (@event.IsActionPressed("ui_cancel"))
            {
                _mouseCapture = !_mouseCapture;
                Input.MouseMode = _mouseCapture 
                    ? Input.MouseModeEnum.Captured 
                    : Input.MouseModeEnum.Visible;
            }
        }

        /// <summary>
        /// Physics-Update für Bewegung
        /// </summary>
        public override void _PhysicsProcess(double delta)
        {
            // Bewegungseingaben sammeln
            Vector3 inputDir = Vector3.Zero;
            
            // WASD-Bewegung
            if (Input.IsActionPressed("move_forward"))
                inputDir.Z -= 1;
            if (Input.IsActionPressed("move_back"))
                inputDir.Z += 1;
            if (Input.IsActionPressed("move_left"))
                inputDir.X -= 1;
            if (Input.IsActionPressed("move_right"))
                inputDir.X += 1;
            if (Input.IsActionPressed("move_up"))
                inputDir.Y += 1;
            if (Input.IsActionPressed("move_down"))
                inputDir.Y -= 1;
            
            // Normalisieren für gleichmäßige Geschwindigkeit
            if (inputDir.Length() > 0)
            {
                inputDir = inputDir.Normalized();
                
                // In lokale Richtung transformieren
                Vector3 direction = Transform.Basis * inputDir;
                
                // Sprint-Multiplikator anwenden
                float currentSpeed = MoveSpeed;
                if (Input.IsActionPressed("sprint"))
                {
                    currentSpeed *= SprintMultiplier;
                }
                
                // Geschwindigkeit setzen
                Velocity = direction * currentSpeed;
            }
            else
            {
                // Stoppen wenn keine Eingabe
                Velocity = Vector3.Zero;
            }
            
            // Bewegung ausführen
            MoveAndSlide();
            
            // VoxelWorld über neue Position informieren
            UpdateWorldPosition();
        }

        /// <summary>
        /// Aktualisiert die Spielerposition in der VoxelWorld für Chunk-Streaming
        /// </summary>
        private void UpdateWorldPosition()
        {
            if (_voxelWorld != null)
            {
                _voxelWorld.UpdatePlayerPosition(GlobalPosition);
            }
            
            // Debug-Info
            if (Time.GetUnixTimeFromSystem() % 60 < 1) // Einmal pro Minute
            {
                GD.Print($"Player Position: {GlobalPosition:F2}");
            }
        }

        /// <summary>
        /// Gibt die aktuelle Kamera-Referenz zurück
        /// </summary>
        public Camera3D GetCamera() => _camera;
    }
}