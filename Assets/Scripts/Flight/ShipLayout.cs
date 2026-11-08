using UnityEngine;

namespace HostileHorizon
{
    // Ship-space points of the Blender ship (Tools/Blender/ship.py: change both together). Ship space = Unity space, metres.
    public static class ShipLayout
    {
        public static readonly Vector3 Eye = new Vector3(0, 1.05f, 2.4f);          // ship.py EYE
        public static readonly Vector3 DashCentre = new Vector3(0, .6f, 2.95f);    // Round 8: the Meshy dash's middle (sounds); was (0, .62, 3.25)
        // Round 8 (B1): the dash's three screen recesses (ship.py prints them: SCREEN ...); the live screens sit in them
        public static readonly Vector3 ScreenCentre = new Vector3(0, .69f, 2.98f), ScreenLeft = new Vector3(-.359f, .676f, 2.975f), ScreenRight = new Vector3(.359f, .676f, 2.975f);

        // points that touch the ground first (Round 8: the Meshy hull's extremes, ship.py prints them: EXTREME ...): nose, under the nose,
        // belly, the body's tail, tail fins' tips and tops, wing tips, engine pods, canopy top
        public static readonly Vector3[] Contacts =
        {
            new Vector3(0, 0, 5.0f), new Vector3(0, -.58f, 4.63f), new Vector3(0, -1.24f, .77f), new Vector3(0, .3f, -4.47f),
            new Vector3(-1.59f, 2.45f, -5.0f), new Vector3(1.59f, 2.45f, -5.0f), new Vector3(-1.61f, 2.66f, -4.09f), new Vector3(1.61f, 2.66f, -4.09f),
            new Vector3(-4.62f, -.1f, -.4f), new Vector3(4.62f, -.1f, -.4f), new Vector3(-2.4f, .5f, -2.75f), new Vector3(2.4f, .5f, -2.75f),
            new Vector3(0, 1.88f, 2.6f), new Vector3(0, -1.0f, -2.0f),
        };
    }
}
