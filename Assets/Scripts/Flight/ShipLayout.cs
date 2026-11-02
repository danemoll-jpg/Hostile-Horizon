using UnityEngine;

namespace HostileHorizon
{
    // Ship-space points of the Blender ship (Tools/Blender/ship.py: change both together). Ship space = Unity space, metres.
    public static class ShipLayout
    {
        public static readonly Vector3 Eye = new Vector3(0, 1.05f, 2.4f);          // ship.py EYE
        public static readonly Vector3 DashCentre = new Vector3(0, .62f, 3.25f);   // ship.py DASH

        // points that touch the ground first: nose, tail, wing tips, belly, canopy top, fin tops, engine pods
        public static readonly Vector3[] Contacts =
        {
            new Vector3(0, 0, 6.2f), new Vector3(0, -.3f, -6.2f), new Vector3(-5.6f, -.75f, -3.7f), new Vector3(5.6f, -.75f, -3.7f),
            new Vector3(0, -.9f, 0), new Vector3(0, -.85f, 3.2f), new Vector3(0, 1.6f, 2.9f), new Vector3(0, 1.15f, -2f),
            new Vector3(-2.85f, 2f, -5.7f), new Vector3(2.85f, 2f, -5.7f), new Vector3(-2.35f, -.8f, -5.5f), new Vector3(2.35f, -.8f, -5.5f),
            new Vector3(-3.5f, -.5f, -1.5f), new Vector3(3.5f, -.5f, -1.5f),
        };
    }
}
