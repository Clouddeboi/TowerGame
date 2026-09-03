namespace Game.Player.Movement
{
    //explicit movement state, PlayerMovementController is the only thing that sets
    //this, everything else (camera, combat) reads it, matches the no-boolean-spaghetti
    //discipline used throughout the inventory system
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Sprinting,
        Jumping,
        Falling,
        Landing,
        Crouching
    }
}