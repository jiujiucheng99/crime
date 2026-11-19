using System.IO;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Builds a minimal movement test scene at runtime:
    /// ground, street, buildings, crates, player, camera and debug HUD.
    /// Press Play in SampleScene; no prefab setup is required yet.
    /// </summary>
    public sealed class MovementTestBootstrap : MonoBehaviour
    {
        private static Sprite _whiteSprite;
        private PlayerMotor2D _player;
        private SafehouseController _safehouse;
        private TransportCenterController _transport;
        private ConvenienceStoreController _convenience;
        private BarController _bar;
        private RestaurantController _restaurant;
        private AutoRepairController _autoRepair;
        private NightclubController _nightclub;
        private WarehouseController _warehouse;
        private PortOfficeController _portOffice;
        private PlayerWallet _wallet;
        private StorageInteraction _storage;
        private Transform _worldRoot;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BuildCamera();
            BuildWorld();
            BuildPlayer();
        }

        private void BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow2D>();
            follow.enabled = false; // target is assigned after the player exists
            _camera = camera;
            _cameraFollow = follow;
        }

        private Camera _camera;
        private CameraFollow2D _cameraFollow;

        private void BuildWorld()
        {
            Transform world = new GameObject("BrickRowBlockout").transform;
            _worldRoot = world;

            // Each block is 16x16, roads are 5 wide. Columns D-H, rows 6-8.
            const float blockSize = 16f;
            const float roadWidth = 6f;
            const float spacing = blockSize + roadWidth; // 22
            float[] centersX = { 0f, spacing, 2f * spacing, 3f * spacing, 4f * spacing };
            float[] centersY = { 11f, -11f, -33f };
            string[] columns = { "D", "E", "F", "G", "H" };

            CreateBlock(world, "Ground", new Vector3(44f, -11f, 0f), new Vector2(140f, 100f),
                new Color(0.055f, 0.060f, 0.070f), false, -20000);

            // Brick Lane at y=0 and South Street at y=-22. The old y=22 road
            // is replaced by the Oren River; a bridge at F connects the districts.
            foreach (float y in new[] { 0f, -22f, -44f })
            {
                CreateBlock(world, "RoadHorizontal", new Vector3(44f, y, 0f), new Vector2(120f, roadWidth),
                    new Color(0.11f, 0.12f, 0.14f), false, -11000);
            }

            // Vertical roads between D/E, E/F, F/G, G/H and at both district edges.
            foreach (float x in new[] { -11f, 11f, 33f, 55f, 77f, 99f })
            {
                CreateBlock(world, "RoadVertical", new Vector3(x, -11f, 0f), new Vector2(roadWidth, 76f),
                    new Color(0.11f, 0.12f, 0.14f), false, -11000);
            }

            // Brick Lane lane markings, matching the restored detailed street.
            BuildRoadMarkings(world);

            BuildCrosswalks(world);

            BuildRiverAndBridge(world);

            Color[] palette =
            {
                new Color(0.36f, 0.22f, 0.19f), new Color(0.30f, 0.23f, 0.20f), new Color(0.28f, 0.23f, 0.21f),
                new Color(0.40f, 0.28f, 0.20f), new Color(0.34f, 0.25f, 0.19f), new Color(0.29f, 0.30f, 0.26f),
                new Color(0.30f, 0.18f, 0.20f), new Color(0.24f, 0.20f, 0.24f), new Color(0.26f, 0.23f, 0.19f),
                new Color(0.35f, 0.29f, 0.22f), new Color(0.29f, 0.25f, 0.22f), new Color(0.23f, 0.25f, 0.23f),
                new Color(0.22f, 0.26f, 0.29f), new Color(0.23f, 0.21f, 0.19f), new Color(0.20f, 0.26f, 0.29f)
            };

            int paletteIndex = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    string blockId = columns[col] + (6 + row);
                    bool detailed = blockId == "D6" || blockId == "E6" || blockId == "F6" || blockId == "G6"
                        || blockId == "D7" || blockId == "E7" || blockId == "G7";
                    CreateBrickRowBlock(world, blockId, centersX[col], centersY[row], palette[paletteIndex], detailed);
                    paletteIndex++;
                }
            }

            // First Gold Arch placeholder block: police station above F6.
            CreateBlock(world, "GroundNorth", new Vector3(44f, 52f, 0f), new Vector2(140f, 26f),
                new Color(0.055f, 0.060f, 0.070f), false, -20000);
            CreatePoliceStationBlock(world);

            // Invisible district bounds.
            CreateBlock(world, "Bounds_W", new Vector3(-14.5f, 0f, 0f), new Vector2(1f, 112f), Color.clear, true, -19999);
            CreateBlock(world, "Bounds_E", new Vector3(102.5f, 0f, 0f), new Vector2(1f, 112f), Color.clear, true, -19999);
            CreateBlock(world, "Bounds_N", new Vector3(44f, 61f, 0f), new Vector2(118f, 1f), Color.clear, true, -19999);
            CreateBlock(world, "Bounds_S", new Vector3(44f, -47.5f, 0f), new Vector2(118f, 1f), Color.clear, true, -19999);
        }

        private void CreatePoliceStationBlock(Transform world)
        {
            GameObject blockRoot = new GameObject("Block_F5_PoliceStation");
            blockRoot.transform.SetParent(world, false);
            blockRoot.transform.position = new Vector3(44f, 45f, 0f);

            CreateBlock(blockRoot.transform, "Sidewalk", new Vector3(44f, 45f, 0f), new Vector2(16f, 16f),
                new Color(0.34f, 0.34f, 0.32f), false, -10500);
            CreateBlock(blockRoot.transform, "SidewalkTop", new Vector3(44f, 51.8f, 0f), new Vector2(16f, 2.4f),
                new Color(0.38f, 0.37f, 0.34f), false, -10400);
            CreateBlock(blockRoot.transform, "SidewalkBottom", new Vector3(44f, 38.2f, 0f), new Vector2(16f, 2.4f),
                new Color(0.38f, 0.37f, 0.34f), false, -10400);
            CreateBlock(blockRoot.transform, "SidewalkLeft", new Vector3(37.2f, 45f, 0f), new Vector2(2.4f, 16f),
                new Color(0.38f, 0.37f, 0.34f), false, -10400);
            CreateBlock(blockRoot.transform, "SidewalkRight", new Vector3(50.8f, 45f, 0f), new Vector2(2.4f, 16f),
                new Color(0.38f, 0.37f, 0.34f), false, -10400);

            // Placeholder solid-colour police station until the real facade is drawn.
            CreateBlock(blockRoot.transform, "PoliceStationPlaceholder", new Vector3(44f, 45f, 0f),
                new Vector2(11.2f, 11.2f), new Color(0.16f, 0.22f, 0.34f), true, -940);
            CreateBlock(blockRoot.transform, "PoliceDoor", new Vector3(44f, 39.45f, 0f),
                new Vector2(2.4f, 0.65f), new Color(0.08f, 0.12f, 0.20f), false, -939);
            CreateBlock(blockRoot.transform, "PoliceSign", new Vector3(44f, 48.5f, 0f),
                new Vector2(3.6f, 0.8f), new Color(0.72f, 0.78f, 0.88f), false, -939);

            WorkAreaMarker.Create(blockRoot.transform, "PoliceStationMarker",
                new Vector3(0f, -3.0f, 0f), new Color(0.45f, 0.72f, 1f));
        }

        private void BuildRoadMarkings(Transform world)
        {
            Color paint = new Color(0.75f, 0.70f, 0.55f);

            // Dashed centre lines on every horizontal road.
            foreach (float roadY in new[] { 34f, 22f, 0f, -22f, -44f })
            {
                for (float x = -6f; x <= 94f; x += 4f)
                {
                    CreateBlock(world, "RoadMarkH", new Vector3(x, roadY, 0f), new Vector2(1.6f, 0.12f),
                        paint, false, -10870);
                }
            }

            // Dashed centre lines on every vertical road.
            foreach (float roadX in new[] { -11f, 11f, 33f, 55f, 77f, 99f })
            {
                for (float y = -40f; y <= 18f; y += 4f)
                {
                    CreateBlock(world, "RoadMarkV", new Vector3(roadX, y, 0f), new Vector2(0.12f, 1.6f),
                        paint, false, -10960);
                }
            }
        }

        private void BuildRiverAndBridge(Transform world)
        {
            Sprite water = Resources.Load<Sprite>("Environment/BrickRow/Port/water_tile");
            Sprite road = Resources.Load<Sprite>("Environment/BrickRow/Roads/road_tile");
            Sprite bridge = Resources.Load<Sprite>("Environment/BrickRow/Roads/bridge_tile");

            // Riverside roads keep sidewalks away from the water.
            CreateTiledSprite(world, "RiversideRoadSouth", road,
                new Vector3(44f, 22f, 0f), new Vector2(140f, 6f), -10880, false);
            CreateTiledSprite(world, "RiversideRoadNorth", road,
                new Vector3(44f, 34f, 0f), new Vector2(140f, 6f), -10880, false);

            // Oren River occupies the band between the two riverside roads.
            if (water != null)
            {
                CreateTiledSprite(world, "OrenRiver", water,
                    new Vector3(44f, 28f, 0f), new Vector2(140f, 6f), -10900, false);
            }
            else
            {
                CreateBlock(world, "OrenRiverFallback", new Vector3(44f, 28f, 0f),
                    new Vector2(140f, 6f), new Color(0.12f, 0.25f, 0.32f), false, -10900);
            }

            // Dark banks frame the water.
            CreateBlock(world, "RiverBankSouth", new Vector3(44f, 25.15f, 0f),
                new Vector2(140f, 0.35f), new Color(0.13f, 0.15f, 0.14f), false, -10890);
            CreateBlock(world, "RiverBankNorth", new Vector3(44f, 30.85f, 0f),
                new Vector2(140f, 0.35f), new Color(0.13f, 0.15f, 0.14f), false, -10890);

            // Water blocks everywhere except the bridge opening at x=40..48.
            CreateBlock(world, "RiverColliderWest", new Vector3(12f, 28f, 0f),
                new Vector2(56f, 6f), Color.clear, true, -19998);
            CreateBlock(world, "RiverColliderEast", new Vector3(76f, 28f, 0f),
                new Vector2(56f, 6f), Color.clear, true, -19998);

            // Bridge spans both roads and the river at F5/F6.
            CreateTiledSprite(world, "BridgeDeck", bridge,
                new Vector3(44f, 28f, 0f), new Vector2(8f, 18f), -10850, false);
            // Railings only protect the water span. The road approaches are
            // open so a vehicle can turn from the riverside road onto the bridge.
            CreateBlock(world, "BridgeRailingWest", new Vector3(40f, 28f, 0f),
                new Vector2(0.22f, 5.7f), new Color(0.34f, 0.35f, 0.34f), true, -10840);
            CreateBlock(world, "BridgeRailingEast", new Vector3(48f, 28f, 0f),
                new Vector2(0.22f, 5.7f), new Color(0.34f, 0.35f, 0.34f), true, -10840);

            for (float y = 20f; y <= 36f; y += 2f)
            {
                CreateBlock(world, "BridgeMark", new Vector3(44f, y, 0f),
                    new Vector2(0.12f, 0.8f), new Color(0.75f, 0.70f, 0.55f), false, -10830);
            }
        }

        private void BuildCrosswalks(Transform world)
        {
            Color paint = new Color(0.78f, 0.77f, 0.70f);
            float[] horizontalRoads = { 0f, -22f };
            float[] columnCenters = { 0f, 22f, 44f, 66f, 88f };

            // Horizontal roads: stripes run parallel to the road (east-west),
            // stacked across it to connect the north/south sidewalks.
            foreach (float roadY in horizontalRoads)
            {
                foreach (float centerX in columnCenters)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float barY = roadY - 2.4f + i * 1.2f;
                        CreateBlock(world, "CrosswalkHorizontal", new Vector3(centerX, barY, 0f),
                            new Vector2(4f, 0.45f), paint, false, -10950);
                    }
                }
            }

            // Vertical roads: stripes run parallel to the road (north-south),
            // stacked across it to connect the east/west sidewalks.
            float[] verticalRoads = { 11f, 33f, 55f, 77f };
            float[] rowCenters = { 11f, -11f, -33f };
            foreach (float roadX in verticalRoads)
            {
                foreach (float centerY in rowCenters)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float barX = roadX - 2.4f + i * 1.2f;
                        CreateBlock(world, "CrosswalkVertical", new Vector3(barX, centerY, 0f),
                            new Vector2(0.45f, 4f), paint, false, -10950);
                    }
                }
            }
        }

        private void BuildYard(Transform blockRoot, float centerX, float centerY, bool north)
        {
            Sprite grass = Resources.Load<Sprite>("Environment/BrickRow/Yard/grass_tile");
            Sprite gravel = Resources.Load<Sprite>("Environment/BrickRow/Yard/gravel_tile");
            Sprite fenceH = Resources.Load<Sprite>("Environment/BrickRow/Yard/fence_h");
            Sprite fenceV = Resources.Load<Sprite>("Environment/BrickRow/Yard/fence_v");
            Sprite gate = Resources.Load<Sprite>("Environment/BrickRow/Yard/gate");
            Sprite shrub = Resources.Load<Sprite>("Environment/BrickRow/Yard/shrub");

            if (grass != null)
            {
                CreateTiledSprite(blockRoot, "YardGrass", grass,
                    new Vector3(centerX, centerY, 0f), new Vector2(16f, 16f), -10450);
            }

            float frontY = centerY + (north ? -5.5f : 5.5f);
            float backY = centerY + (north ? 5.5f : -5.5f);
            float direction = north ? 1f : -1f;

            if (gravel != null)
            {
                CreateTiledSprite(blockRoot, "YardPath", gravel,
                    new Vector3(centerX, frontY + direction * 1f, 0f), new Vector2(1.2f, 2f), -10440);
            }

            if (fenceH != null)
            {
                int frontOrder = Mathf.RoundToInt(-frontY * 100f);
                CreateTiledSprite(blockRoot, "FenceFrontLeft", fenceH,
                    new Vector3(centerX - 3.15f, frontY, 0f), new Vector2(4.9f, 0.5f), frontOrder, true);
                CreateTiledSprite(blockRoot, "FenceFrontRight", fenceH,
                    new Vector3(centerX + 3.15f, frontY, 0f), new Vector2(4.9f, 0.5f), frontOrder, true);
                CreateTiledSprite(blockRoot, "FenceBack", fenceH,
                    new Vector3(centerX, backY, 0f), new Vector2(11.2f, 0.5f), Mathf.RoundToInt(-backY * 100f), true);
            }

            if (fenceV != null)
            {
                int sideOrder = Mathf.RoundToInt(-((frontY + backY) * 0.5f) * 100f);
                float fenceCenterY = (frontY + backY) * 0.5f;
                CreateTiledSprite(blockRoot, "FenceSideLeft", fenceV,
                    new Vector3(centerX - 5.6f, fenceCenterY, 0f), new Vector2(0.5f, 11f), sideOrder, true);
                CreateTiledSprite(blockRoot, "FenceSideRight", fenceV,
                    new Vector3(centerX + 5.6f, fenceCenterY, 0f), new Vector2(0.5f, 11f), sideOrder, true);
            }

            if (gate != null)
            {
                CreateTiledSprite(blockRoot, "YardGate", gate,
                    new Vector3(centerX, frontY, 0f), new Vector2(1.4f, 0.5f), Mathf.RoundToInt(-frontY * 100f) + 1);
            }

            if (shrub != null)
            {
                CreateTiledSprite(blockRoot, "YardShrubLeft", shrub,
                    new Vector3(centerX - 4.7f, frontY + direction * 1.15f, 0f), new Vector2(1f, 0.75f), -10430);
                CreateTiledSprite(blockRoot, "YardShrubRight", shrub,
                    new Vector3(centerX + 4.7f, frontY + direction * 1.15f, 0f), new Vector2(1f, 0.75f), -10430);
            }
        }

        private static GameObject CreateTiledSprite(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 size, int sortingOrder, bool collider = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;

            if (collider)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = size;
            }

            return go;
        }

        private static void CreateBusStopSign(Transform parent, float x, float y)
        {
            CreateBlock(parent, "BusStopPole", new Vector3(x, y, 0f), new Vector2(0.1f, 1.2f),
                new Color(0.16f, 0.18f, 0.19f), false, -10390);
            CreateBlock(parent, "BusStopBoard", new Vector3(x, y + 0.9f, 0f), new Vector2(1.0f, 0.48f),
                new Color(0.16f, 0.32f, 0.42f), false, -10389);
            CreateBlock(parent, "BusStopBoardTrim", new Vector3(x, y + 0.68f, 0f), new Vector2(0.82f, 0.08f),
                new Color(0.82f, 0.62f, 0.24f), false, -10388);
        }

        private static void BuildSidewalkProps(Transform parent, string blockId, float centerX, float centerY)
        {
            // D6/D7 already have their own detailed street props.
            if (blockId == "D6" || blockId == "D7") return;

            if (blockId == "H8")
            {
                StreetPropFactory.Streetlight(parent, new Vector2(centerX - 6.5f, centerY - 3f));
                StreetPropFactory.Streetlight(parent, new Vector2(centerX - 3f, centerY + 6.5f));
                StreetPropFactory.Hydrant(parent, new Vector2(centerX - 6.5f, centerY + 5.8f));
                StreetPropFactory.TrashCan(parent, new Vector2(centerX - 6.5f, centerY - 5.8f));
                return;
            }

            StreetPropFactory.Streetlight(parent, new Vector2(centerX - 6.5f, centerY + 3f));
            StreetPropFactory.Streetlight(parent, new Vector2(centerX + 6.5f, centerY - 3f));
            StreetPropFactory.Hydrant(parent, new Vector2(centerX + 6.5f, centerY + 5.8f));
            StreetPropFactory.TrashCan(parent, new Vector2(centerX - 6.5f, centerY - 5.8f));

            int column = blockId[0] - 'D';
            if (column % 2 == 0)
            {
                StreetPropFactory.ParkingMeter(parent, new Vector2(centerX + 6.5f, centerY + 1f));
            }
        }

        private static bool TryCreateBusinessBuilding(Transform parent, string blockId, float centerX, float centerY)
        {
            string resourcePath = null;
            Color color = new Color(0.28f, 0.28f, 0.28f);

            if (blockId == "D8") { resourcePath = "Environment/BrickRow/Businesses/convenience_store"; color = new Color(0.34f, 0.46f, 0.31f); }
            else if (blockId == "E8") { resourcePath = "Environment/BrickRow/Businesses/restaurant"; color = new Color(0.55f, 0.35f, 0.24f); }
            else if (blockId == "F8") { resourcePath = "Environment/BrickRow/Businesses/bar"; color = new Color(0.31f, 0.25f, 0.35f); }
            else if (blockId == "H6") { resourcePath = "Environment/BrickRow/Businesses/auto_repair"; color = new Color(0.30f, 0.34f, 0.38f); }
            else if (blockId == "H7") { resourcePath = "Environment/BrickRow/Businesses/nightclub"; color = new Color(0.27f, 0.20f, 0.32f); }
            else if (blockId == "G8") { resourcePath = "Environment/BrickRow/Businesses/warehouse"; color = new Color(0.38f, 0.31f, 0.25f); }
            else if (blockId == "H8") { resourcePath = "Environment/BrickRow/Businesses/port"; color = new Color(0.22f, 0.28f, 0.32f); }
            else return false;

            Sprite sprite = string.IsNullOrEmpty(resourcePath) ? null : Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                CreateImageBuilding(parent, "Business_" + blockId, resourcePath, centerX, centerY, 11.2f, -940);
            }
            else
            {
                CreateBlock(parent, "Business_" + blockId, new Vector3(centerX, centerY, 0f),
                    new Vector2(11.2f, 11.2f), color, true, -940);
            }
            return true;
        }

        private static void BuildPortDecor(Transform parent, float centerX, float centerY)
        {
            Sprite small = Resources.Load<Sprite>("Environment/BrickRow/Port/boat_small");
            Sprite fishing = Resources.Load<Sprite>("Environment/BrickRow/Port/boat_fishing");
            Sprite cargo = Resources.Load<Sprite>("Environment/BrickRow/Port/boat_cargo");

            CreateBoat(parent, "BoatSmall", small, new Vector3(centerX + 20f, centerY + 8f, 0f), 2.2f, false);
            CreateBoat(parent, "BoatFishing", fishing, new Vector3(centerX + 29f, centerY - 6f, 0f), 2.8f, true);
            CreateBoat(parent, "BoatCargo", cargo, new Vector3(centerX + 27f, centerY - 17f, 0f), 3.6f, false);
            // Small boat inside the port's own water strip.
            CreateBoat(parent, "BoatInPort", small, new Vector3(centerX + 3.9f, centerY - 2f, 0f), 1.5f, false, -930);
        }

        private static void CreateBoat(Transform parent, string name, Sprite sprite, Vector3 position, float width, bool flip, int sortingOrder = -11040)
        {
            if (sprite == null) return;

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = flip;
            float scale = width / Mathf.Max(0.001f, sprite.bounds.size.x);
            root.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void BuildPortWater(Transform parent, float centerX, float centerY)
        {
            Sprite water = Resources.Load<Sprite>("Environment/BrickRow/Port/water_tile");
            if (water == null) return;

            // Water leaves the port's right edge, passes under the east road,
            // then opens into the lake east of Brick Row.
            CreateTiledSprite(parent, "PortChannel", water,
                new Vector3(centerX + 9.8f, centerY, 0f), new Vector2(8.4f, 11.2f), -11050);
            CreateTiledSprite(parent, "PortLake", water,
                new Vector3(centerX + 30f, centerY, 0f), new Vector2(32f, 50f), -11050);
        }

        private static void CreateHomeMarker(Transform parent, float centerX, float centerY)
        {
            Sprite sprite = Resources.Load<Sprite>("Environment/BrickRow/Markers/home_icon");
            if (sprite == null) return;

            GameObject root = new GameObject("HomeMarker");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, centerY, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -930;
            float scale = 1f / Mathf.Max(0.001f, sprite.bounds.size.x);
            visual.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void CreateImageBuilding(Transform parent, string name, string resourcePath,
            float centerX, float centerY, float size, int sortingOrder)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                CreateBlock(parent, name + "_Fallback", new Vector3(centerX, centerY, 0f),
                    new Vector2(size, size), new Color(0.28f, 0.26f, 0.24f), true, sortingOrder);
                return;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, centerY, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            float scale = size / Mathf.Max(0.001f, sprite.bounds.size.x);
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
        }

        private void CreateBrickRowBlock(Transform world, string blockId, float centerX, float centerY, Color buildingColor, bool isDetailedBlock)
        {
            GameObject blockRoot = new GameObject("Block_" + blockId);
            blockRoot.transform.SetParent(world, false);
            blockRoot.transform.position = new Vector3(centerX, centerY, 0f);

            BrickRowBlock block = blockRoot.AddComponent<BrickRowBlock>();
            block.Configure(blockId, blockId[0] - 'D', int.Parse(blockId.Substring(1)) - 6, buildingColor);

            // Block ground / sidewalk pad.
            CreateBlock(blockRoot.transform, "Sidewalk", new Vector3(centerX, centerY, 0f), new Vector2(16f, 16f),
                new Color(0.34f, 0.34f, 0.32f), false, -10500);

            // Sidewalk strips along the top and bottom edges of every grey block.
            CreateSidewalk(blockRoot.transform, "SidewalkTop", new Vector3(centerX, centerY + 6.8f, 0f),
                new Vector2(16f, 2.4f), -10400);
            CreateSidewalk(blockRoot.transform, "SidewalkBottom", new Vector3(centerX, centerY - 6.8f, 0f),
                new Vector2(16f, 2.4f), -10400);

            // Left/right edges as well, completing the sidewalk ring.
            CreateSidewalk(blockRoot.transform, "SidewalkLeft", new Vector3(centerX - 6.8f, centerY, 0f),
                new Vector2(2.4f, 16f), -10400);
            if (blockId == "H8")
            {
                // Restore the right walkway as two quay segments, leaving a
                // central opening for the water connection.
                CreateSidewalk(blockRoot.transform, "SidewalkRightTop", new Vector3(centerX + 6.8f, centerY + 5.8f, 0f),
                    new Vector2(2.4f, 4.4f), -10400);
                CreateSidewalk(blockRoot.transform, "SidewalkRightBottom", new Vector3(centerX + 6.8f, centerY - 5.8f, 0f),
                    new Vector2(2.4f, 4.4f), -10400);
            }
            else
            {
                CreateSidewalk(blockRoot.transform, "SidewalkRight", new Vector3(centerX + 6.8f, centerY, 0f),
                    new Vector2(2.4f, 16f), -10400);
            }

            BuildSidewalkProps(blockRoot.transform, blockId, centerX, centerY);

            // The centre block is the bus transport centre. Its image fills the
            // full 11.2x11.2 grey area inside the sidewalk ring.
            if (blockId == "F7")
            {
                CreateImageBuilding(blockRoot.transform, "BusTransportCenter",
                    "Environment/BrickRow/Transport/bus_transport_center",
                    centerX, centerY, 11.2f, -940);
                return;
            }

            // Named service buildings: full 11.2x11.2 grey footprint.
            if (TryCreateBusinessBuilding(blockRoot.transform, blockId, centerX, centerY))
            {
                if (blockId == "H8")
                {
                    BuildPortWater(blockRoot.transform, centerX, centerY);
                    BuildPortDecor(blockRoot.transform, centerX, centerY);
                    CreateBusStopSign(blockRoot.transform, centerX - 4f, centerY + 6.8f);
                }
                return;
            }

            if (isDetailedBlock)
            {
                bool north = blockId == "D6" || blockId == "E6" || blockId == "F6" || blockId == "G6";
                float apartmentY = centerY;
                string resourcePath = north ? "Environment/BrickRow/apartment_north" : "Environment/BrickRow/apartment_south";
                CreateApartment(blockRoot.transform, (north ? "ApartmentNorth_" : "ApartmentSouth_") + blockId,
                    resourcePath, centerX, apartmentY, north ? -950 : -951, north ? 8f : 7.2f);

                float curbY = north ? 3.05f : -3.05f;
                CreateBlock(blockRoot.transform, "Curb", new Vector3(centerX, curbY, 0f), new Vector2(16f, 0.12f),
                    new Color(0.62f, 0.62f, 0.58f), false, -10300);

                if (blockId == "D6")
                {
                    CreateBlock(blockRoot.transform, "ApartmentDoor", new Vector3(centerX, centerY - 3.6f, 0f), new Vector2(1.4f, 0.3f),
                        new Color(0.08f, 0.065f, 0.055f), false, -9990);
                    CreateHomeMarker(blockRoot.transform, centerX, centerY - 0.8f);
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(-6f, 3.4f));
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(6f, 3.4f));
                    StreetPropFactory.TrashCan(blockRoot.transform, new Vector2(-5.5f, 4.7f));
                    StreetPropFactory.Bench(blockRoot.transform, new Vector2(5.5f, 4.6f));
                    StreetPropFactory.Hydrant(blockRoot.transform, new Vector2(-2.5f, 3.4f));
                }
                else if (north)
                {
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(-6f, 3.4f));
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(6f, 3.4f));
                    StreetPropFactory.TrashCan(blockRoot.transform, new Vector2(-5.5f, 4.7f));
                    StreetPropFactory.Bench(blockRoot.transform, new Vector2(5.5f, 4.6f));
                    StreetPropFactory.Hydrant(blockRoot.transform, new Vector2(-2.5f, 3.4f));
                }
                else
                {
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(-6f, -3.4f));
                    StreetPropFactory.Streetlight(blockRoot.transform, new Vector2(6f, -3.4f));
                    StreetPropFactory.TrashCan(blockRoot.transform, new Vector2(5.5f, -4.7f));
                    StreetPropFactory.BenchBack(blockRoot.transform, new Vector2(-5.5f, -4.6f));
                    StreetPropFactory.Hydrant(blockRoot.transform, new Vector2(2.5f, -3.4f));
                }

                BuildYard(blockRoot.transform, centerX, centerY, north);
                return;
            }

            // One larger solid placeholder building centred in the grey block.
            CreateBlock(blockRoot.transform, "BuildingMain", new Vector3(centerX, centerY, 0f), new Vector2(8f, 7f),
                buildingColor, true, -10000);
        }

        private void BuildStreetDetails(Transform world)
        {
            // Curbs along both sides of the road.
            CreateBlock(world, "CurbNorth", new Vector3(0f, 3.05f, 0f), new Vector2(60f, 0.12f),
                new Color(0.62f, 0.62f, 0.58f), false, -880);
            CreateBlock(world, "CurbSouth", new Vector3(0f, -3.05f, 0f), new Vector2(60f, 0.12f),
                new Color(0.56f, 0.56f, 0.52f), false, -880);

            // Crosswalks at the west and east ends. Stripes run parallel to the road
            // (horizontal on screen), stacked across the road.
            for (int i = 0; i < 5; i++)
            {
                float y = -2.4f + i * 1.2f;
                CreateBlock(world, "CrosswalkWest", new Vector3(-24f, y, 0f), new Vector2(4f, 0.45f),
                    new Color(0.78f, 0.77f, 0.70f), false, -870);
                CreateBlock(world, "CrosswalkEast", new Vector3(24f, y, 0f), new Vector2(4f, 0.45f),
                    new Color(0.78f, 0.77f, 0.70f), false, -870);
            }

            // Ground decals.
            // Only the four requested prop types remain in this detail pass.

            // North sidewalk: protagonist apartment side.
            StreetPropFactory.Streetlight(world, new Vector2(-20f, 3.4f));
            StreetPropFactory.Streetlight(world, new Vector2(-8f, 3.4f));
            StreetPropFactory.Streetlight(world, new Vector2(8f, 3.4f));
            StreetPropFactory.Streetlight(world, new Vector2(20f, 3.4f));
            StreetPropFactory.TrashCan(world, new Vector2(-5.5f, 4.7f));
            StreetPropFactory.Bench(world, new Vector2(5.5f, 4.6f));
            StreetPropFactory.Hydrant(world, new Vector2(-2.5f, 3.4f));

            // South sidewalk: neighbouring residence side.
            StreetPropFactory.Streetlight(world, new Vector2(-16f, -3.4f));
            StreetPropFactory.Streetlight(world, new Vector2(16f, -3.4f));
            StreetPropFactory.TrashCan(world, new Vector2(7f, -4.7f));
            StreetPropFactory.BenchBack(world, new Vector2(-8f, -4.6f));
            StreetPropFactory.Hydrant(world, new Vector2(10f, -3.4f));
        }

        private void BuildPlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -1.5f, 0f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 0.35f);
            collider.offset = new Vector2(0f, 0.175f);

            player.AddComponent<PlayerMotor2D>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.AddComponent<SpriteRenderer>();

            player.AddComponent<PlayerVisuals2D>();
            _player = player.GetComponent<PlayerMotor2D>();

            PlayerInventory pockets = player.AddComponent<PlayerInventory>();
            if (pockets.Count == 0) pockets.TryAdd(new ItemStack("test_item", "TEST ITEM"));
            pockets.TryAdd(new ItemStack("contraband", "CONTRABAND"));
            PlayerWallet wallet = player.AddComponent<PlayerWallet>();
            _wallet = wallet;
            player.AddComponent<PlayerSkills>();
            player.AddComponent<PlayerEmployment>();
            StorageInteraction storage = player.AddComponent<StorageInteraction>();
            storage.Configure(pockets);
            _storage = storage;

            SafehouseController safehouse = gameObject.AddComponent<SafehouseController>();
            safehouse.Configure(_worldRoot, player.transform, new Vector2(0f, 7.4f));
            _safehouse = safehouse;

            BusTravelController busTravel = gameObject.AddComponent<BusTravelController>();
            busTravel.Configure(_worldRoot, player.transform);

            DialogueChoiceController dialogue = gameObject.AddComponent<DialogueChoiceController>();
            dialogue.Configure(player.transform);
            if (safehouse != null) safehouse.SetDialogue(dialogue);

            CrimeLoopController crimeLoop = gameObject.AddComponent<CrimeLoopController>();
            crimeLoop.Configure(player.transform, _worldRoot, pockets, wallet, dialogue);
            gameObject.AddComponent<SkillNotificationUI>();

#if UNITY_EDITOR
            gameObject.AddComponent<PlaytestPanel>();
#endif

            TransportCenterController transport = gameObject.AddComponent<TransportCenterController>();
            transport.Configure(_worldRoot, player.transform, new Vector2(44f, -16.7f), busTravel, dialogue);
            _transport = transport;

            BusStopController busStop = gameObject.AddComponent<BusStopController>();
            busStop.Configure(player.transform, busTravel, new Vector2(82f, -26.2f));

            StoreShopUI shop = gameObject.AddComponent<StoreShopUI>();
            NpcDialogueComponent clerkDialogue = NpcDialogueComponent.Create(transform, "StoreClerkDialogue", dialogue, "店员");
            shop.Configure(player.transform, wallet, clerkDialogue);

            FenceUI fenceUI = gameObject.AddComponent<FenceUI>();
            fenceUI.Configure(player.transform);

            ConvenienceStoreController convenience = gameObject.AddComponent<ConvenienceStoreController>();
            convenience.Configure(_worldRoot, player.transform, new Vector2(0f, -38.8f), dialogue, shop);
            _convenience = convenience;

            BarController bar = gameObject.AddComponent<BarController>();
            bar.Configure(_worldRoot, player.transform, new Vector2(44f, -38.8f), dialogue, wallet);
            _bar = bar;

            RestaurantController restaurant = gameObject.AddComponent<RestaurantController>();
            restaurant.Configure(_worldRoot, player.transform, new Vector2(22f, -38.8f), dialogue);
            _restaurant = restaurant;

            AutoRepairController autoRepair = gameObject.AddComponent<AutoRepairController>();
            autoRepair.Configure(_worldRoot, player.transform, new Vector2(88f, 5.2f), dialogue);
            _autoRepair = autoRepair;

            NightclubController nightclub = gameObject.AddComponent<NightclubController>();
            nightclub.Configure(_worldRoot, player.transform, new Vector2(88f, -16.7f));
            _nightclub = nightclub;

            WarehouseController warehouse = gameObject.AddComponent<WarehouseController>();
            warehouse.Configure(_worldRoot, player.transform, new Vector2(66f, -38.8f), dialogue);
            _warehouse = warehouse;

            PortOfficeController portOffice = gameObject.AddComponent<PortOfficeController>();
            portOffice.Configure(_worldRoot, player.transform, new Vector2(88f, -38.8f), dialogue);
            _portOffice = portOffice;

            CreateNeighborHome("D7", "邻居1的家", new Vector2(0f, -7.25f), new Vector3(4000f, 0f, 0f),
                new Color(0.72f, 0.82f, 1f, 1f), new Color(1f, 0.62f, 0.56f, 1f), safehouse, dialogue, player.transform);
            CreateNeighborHome("E6", "邻居2的家", new Vector2(22f, 7.25f), new Vector3(4200f, 0f, 0f),
                new Color(0.95f, 0.84f, 0.62f, 1f), new Color(0.58f, 0.72f, 0.98f, 1f), safehouse, dialogue, player.transform);
            CreateNeighborHome("F6", "邻居3的家", new Vector2(44f, 7.25f), new Vector3(4400f, 0f, 0f),
                new Color(0.82f, 0.72f, 0.98f, 1f), new Color(1f, 0.68f, 0.42f, 1f), safehouse, dialogue, player.transform);
            CreateNeighborHome("G6", "邻居4的家", new Vector2(66f, 7.25f), new Vector3(4600f, 0f, 0f),
                new Color(0.62f, 0.90f, 0.84f, 1f), new Color(0.96f, 0.86f, 0.42f, 1f), safehouse, dialogue, player.transform);
            CreateNeighborHome("E7", "邻居5的家", new Vector2(22f, -7.25f), new Vector3(4800f, 0f, 0f),
                new Color(0.96f, 0.72f, 0.64f, 1f), new Color(0.72f, 0.58f, 0.94f, 1f), safehouse, dialogue, player.transform);
            CreateNeighborHome("G7", "邻居6的家", new Vector2(66f, -7.25f), new Vector3(5000f, 0f, 0f),
                new Color(0.72f, 0.78f, 0.88f, 1f), new Color(0.92f, 0.55f, 0.80f, 1f), safehouse, dialogue, player.transform);

            if (_cameraFollow != null)
            {
                _cameraFollow.SetTarget(player.transform);
                _cameraFollow.enabled = true;
            }
            if (_camera != null)
            {
                _camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 0.75f, -10f);
            }

            WriteLog("MovementTestBootstrap built. Player at " + player.transform.position);
        }

        private static void CreateSidewalk(Transform parent, string name, Vector3 position, Vector2 size, int sortingOrder)
        {
            Sprite tile = Resources.Load<Sprite>("Environment/BrickRow/sidewalk_tile");
            if (tile == null)
            {
                WriteLog("Missing sidewalk tile; using flat fallback.");
                CreateBlock(parent, name + "_Fallback", position, size, new Color(0.33f, 0.34f, 0.36f), false, sortingOrder);
                return;
            }

            GameObject sidewalk = new GameObject(name);
            sidewalk.transform.SetParent(parent, false);
            sidewalk.transform.position = position;

            SpriteRenderer renderer = sidewalk.AddComponent<SpriteRenderer>();
            renderer.sprite = tile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;
        }

        private static void CreateApartment(Transform parent, string name, string resourcePath, float centerX, float centerY, int sortingOrder, float targetHeight)
        {
            string key = name.ToLowerInvariant().Contains("north") ? "north" : "south";
            Sprite[] available = Resources.LoadAll<Sprite>("Environment/BrickRow");
            Sprite sprite = null;
            foreach (Sprite candidate in available)
            {
                if (candidate.name.ToLowerInvariant().Contains(key))
                {
                    sprite = candidate;
                    break;
                }
            }

            if (sprite == null)
            {
                WriteLog("Missing apartment sprite: " + resourcePath + " — available=" + available.Length + " — using fallback block.");
                CreateBlock(parent, name + "_Fallback", new Vector3(centerX, centerY, 0f), new Vector2(8f, 8f),
                    new Color(0.30f, 0.20f, 0.18f), true, sortingOrder);
                return;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, centerY, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // Keep every building frame 8 units wide; only the south building is
            // compressed vertically to 7.2 so its sides stay parallel to the others.
            float scaleX = 8f / Mathf.Max(0.001f, sprite.bounds.size.x);
            float scaleY = targetHeight / Mathf.Max(0.001f, sprite.bounds.size.y);
            visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Align the sprite bottom to the 7.2-high collider bottom.
            float localY = -3.6f - sprite.bounds.min.y * scaleY;
            visual.transform.localPosition = new Vector3(0f, localY, 0f);

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(7.2f, 7.2f);

            WriteLog("Apartment loaded: " + resourcePath + " sprite=" + sprite.name + " size=" + sprite.rect.size + " ppu=" + sprite.pixelsPerUnit + " targetHeight=" + targetHeight);
        }

        private static void CreateBuilding(Transform parent, string name, Vector3 position, Vector2 size, Color color)
        {
            GameObject building = CreateBlock(parent, name, position, size, color, true, -950);
            SpriteRenderer renderer = building.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = color;
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector2 size,
            Color color,
            bool collider,
            int sortingOrder)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.position = position;

            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            if (collider)
            {
                BoxCollider2D box = block.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
            }

            return block;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;

            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            _whiteSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            _whiteSprite.name = "Prototype_White";
            return _whiteSprite;
        }

        private void OnGUI()
        {
            GUI.depth = 0;
            if (CookingMinigameUI.IsOpen || RepairMinigameUI.IsOpen) return;
            if (BusTravelController.IsOpen || DialogueChoiceController.IsOpen || StoreShopUI.IsOpen) return;
            if (FenceUI.IsOpen) return;
            if (BusDrivingController.Active != null)
            {
                BusDrivingController busDrive = BusDrivingController.Active;
                GUILayout.BeginArea(new Rect(12f, Screen.height - 92f, 560f, 72f), GUI.skin.box);
                GUILayout.Label("BUS DRIVING   speed " + busDrive.Speed.ToString("F1"));
                GUILayout.Label("W: throttle   S: reverse   A/D: steer   Space: brake   F: exit");
                GUILayout.EndArea();
                return;
            }

            // Storage panel owns the screen while it is open; hide the debug HUD.
            if (_storage != null && _storage.IsOpen) return;

            NeighborApartmentController neighbor = NeighborApartmentController.Active;
            TransportDepotController depot = TransportDepotController.Active;
            RestaurantKitchenController kitchen = RestaurantKitchenController.Active;
            GUILayout.BeginArea(new Rect(12f, 12f, 480f, 150f), GUI.skin.box);
            GUILayout.Label(_safehouse != null && _safehouse.IsInside
                ? (_safehouse.IsInBathroom ? "SAFEHOUSE - D6 / Bathroom" : "SAFEHOUSE - D6 / Furnished Greybox")
                : _transport != null && _transport.IsInside
                    ? (depot != null ? "TRANSPORT DEPOT - F7" : "TRANSPORT CENTER - F7 / Lobby")
                    : _convenience != null && _convenience.IsInside
                        ? "CONVENIENCE STORE - D8"
                        : _bar != null && _bar.IsInside
                            ? "BAR - F8"
                            : _restaurant != null && _restaurant.IsInside
                                ? (kitchen != null ? "RESTAURANT KITCHEN - E8" : "RESTAURANT - E8")
                                : _autoRepair != null && _autoRepair.IsInside
                                    ? (_autoRepair.IsShiftActive ? "AUTO REPAIR SHIFT - H6" : "AUTO REPAIR - H6")
                                    : _nightclub != null && _nightclub.IsInside
                                        ? "NIGHTCLUB - H7"
                                        : _warehouse != null && _warehouse.IsInside
                                            ? (_warehouse.IsShiftActive ? "WAREHOUSE SHIFT - G8" : "WAREHOUSE - G8")
                                            : _portOffice != null && _portOffice.IsInside
                                                ? "PORT OFFICE - H8"
                                                : neighbor != null
                                                    ? (neighbor.IsInBathroom ? "NEIGHBOR HOME / Bathroom" : "NEIGHBOR HOME")
                                                    : "BRICK ROW / D6 detailed core + blockout");
            GUILayout.Label(_safehouse != null && _safehouse.IsInside
                ? (_safehouse.IsInBathroom ? "Toilet, sink, mirror and shower" : "Bed, table + chair, bathroom, window and clock")
                : _transport != null && _transport.IsInside
                    ? (depot != null
                        ? "Scene only: large depot with parked buses"
                        : "Reception desk, staff, ticket machines, waiting area and staff-only door")
                    : _convenience != null && _convenience.IsInside
                        ? "Sales floor, shelves, drinks fridge, checkout and stockroom passage"
                        : _bar != null && _bar.IsInside
                            ? "Bar counter, back bar, stools, booths and neon sign"
                            : _restaurant != null && _restaurant.IsInside
                                ? (kitchen != null
                                    ? "Fridge: ingredients  |  Stove: start cooking"
                                    : "Scene only: dining tables, booths, host counter and staff passage")
                                : _autoRepair != null && _autoRepair.IsInside
                                    ? "Workbench: work order  |  Parts shelf: parts  |  Car: repair"
                                    : _nightclub != null && _nightclub.IsInside
                                        ? "Scene only: dance floor, DJ booth, bar, VIP sofas and speakers"
                                        : _warehouse != null && _warehouse.IsInside
                                            ? "Desk: work order  |  Carry A/B/C crates to matching zones"
                                            : _portOffice != null && _portOffice.IsInside
                                                ? "Port foreman only: warehouse hiring"
                                                : neighbor != null
                                                    ? (neighbor.IsInBathroom
                                                        ? "Copied bathroom from the player's D6 home"
                                                        : "Copied apartment layout from the player's D6 home")
                                                    : "D6 safehouse   E6 market   F6 club   F7 gun shop   H8 docks");
            GUILayout.Label(_transport != null && _transport.IsInside
                ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                : _convenience != null && _convenience.IsInside
                    ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                    : _bar != null && _bar.IsInside
                        ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                        : _restaurant != null && _restaurant.IsInside
                            ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                            : _autoRepair != null && _autoRepair.IsInside
                                ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                                : _nightclub != null && _nightclub.IsInside
                                    ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                                    : _warehouse != null && _warehouse.IsInside
                                        ? "WASD: move   Shift: sprint   F: exit to street   E: inventory"
                                        : _portOffice != null && _portOffice.IsInside
                                            ? "WASD: move   Shift: sprint   F: talk / exit   E: inventory"
                                            : neighbor != null
                                ? "WASD: move   Shift: sprint   C: hold to search / lockpick   F: interact / exit   E: inventory"
                                : "WASD: move   Shift: sprint   F: enter/leave apartment   E: inventory   Collisions: buildings");
            GUILayout.Label(PlayerVisuals2D.DebugSummary);
            if (_wallet != null) GUILayout.Label("现金: " + _wallet.Money + " 块");
            if (_player != null)
            {
                GUILayout.Label("velocity: " + _player.Velocity.ToString("F2") + "   sprint: " + _player.IsSprinting);
            }
            GUILayout.EndArea();
        }

        private NeighborApartmentController CreateNeighborHome(string id, string displayName, Vector2 door,
            Vector3 interiorOffset, Color wallTint, Color bedTint,
            SafehouseController safehouse, DialogueChoiceController dialogue, Transform player)
        {
            GameObject go = new GameObject("NeighborHome_" + id);
            go.transform.SetParent(transform, false);
            NeighborApartmentController controller = go.AddComponent<NeighborApartmentController>();
            controller.Configure(_worldRoot, player, door, safehouse, dialogue, displayName, interiorOffset, wallTint, bedTint);
            return controller;
        }

        private static void WriteLog(string message)
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ambercity_movement.log"));
                File.AppendAllText(path, message + "\n");
            }
            catch
            {
                // Prototype logging must never break the scene.
            }
        }
    }

    /// <summary>
    /// Auto-start the movement test when Play is pressed in any scene.
    /// Remove or guard this once a dedicated movement test scene exists.
    /// </summary>
    public static class MovementTestAutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<MovementTestBootstrap>() != null) return;

            GameObject root = new GameObject("MovementTestBootstrap");
            root.AddComponent<MovementTestBootstrap>();
        }
    }
}
