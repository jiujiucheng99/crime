#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AmberCity.EditorTools
{
    /// <summary>
    /// Forces the two apartment PNGs to import as point-filtered pixel sprites at 32 PPU.
    /// This keeps the runtime map assets consistent without manual Inspector work.
    /// </summary>
    public sealed class EnvironmentArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Environment/BrickRow/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            Apply((TextureImporter)assetImporter);
        }

        [InitializeOnLoadMethod]
        private static void EnsureSettings()
        {
            string[] paths =
            {
                Root + "apartment_north.png",
                Root + "apartment_south.png",
                Root + "sidewalk_tile.png",
                Root + "Props/streetlight.png",
                Root + "Props/trashcan.png",
                Root + "Props/bench.png",
                Root + "Props/bench_back.png",
                Root + "Props/hydrant.png",
                Root + "Props/safe.png",
                Root + "Props/safe_open.png",
                Root + "Interior/wall_tile.png",
                Root + "Interior/bed.png",
                Root + "Interior/table_chair.png",
                Root + "Interior/window.png",
                Root + "Interior/clock.png",
                Root + "Interior/bathroom_tile.png",
                Root + "Interior/toilet.png",
                Root + "Interior/sink.png",
                Root + "Interior/mirror.png",
                Root + "Interior/bathroom_door.png",
                Root + "Interior/shower.png",
                Root + "Yard/grass_tile.png",
                Root + "Yard/gravel_tile.png",
                Root + "Yard/fence_h.png",
                Root + "Yard/fence_v.png",
                Root + "Yard/gate.png",
                Root + "Yard/shrub.png",
                Root + "Transport/bus_transport_center.png",
                Root + "Markers/home_icon.png",
                Root + "Businesses/convenience_store.png",
                Root + "Businesses/restaurant.png",
                Root + "Businesses/bar.png",
                Root + "Businesses/auto_repair.png",
                Root + "Businesses/nightclub.png",
                Root + "Businesses/warehouse.png",
                Root + "Businesses/port.png",
                Root + "Port/water_tile.png",
                Root + "Port/boat_small.png",
                Root + "Port/boat_fishing.png",
                Root + "Port/boat_cargo.png",
                Root + "Transport/Interior/station_floor.png",
                Root + "Transport/Interior/station_wall.png",
                Root + "Transport/Interior/reception_desk.png",
                Root + "Transport/Interior/staff.png",
                Root + "Transport/Interior/ticket_machine.png",
                Root + "Transport/Interior/station_bench.png",
                Root + "Transport/Interior/employee_door.png",
                Root + "Transport/Interior/plant.png",
                Root + "Transport/Interior/depot_bus.png",
                Root + "Transport/Vehicles/bus_south.png",
                Root + "Vehicles/police_car.png",
                Root + "Roads/road_tile.png",
                Root + "Roads/bridge_tile.png",
                Root + "Transport/Vehicles/bus_north.png",
                Root + "Transport/Vehicles/bus_east.png",
                Root + "Transport/Vehicles/bus_west.png",
                Root + "Transport/Vehicles/bus_ne.png",
                Root + "Transport/Vehicles/bus_nw.png",
                Root + "Transport/Vehicles/bus_se.png",
                Root + "Transport/Vehicles/bus_sw.png",
                Root + "Businesses/ConvenienceStore/store_floor.png",
                Root + "Businesses/ConvenienceStore/store_wall.png",
                Root + "Businesses/ConvenienceStore/store_shelf.png",
                Root + "Businesses/ConvenienceStore/store_counter.png",
                Root + "Businesses/ConvenienceStore/store_fridge.png",
                Root + "Businesses/ConvenienceStore/store_clerk.png",
                Root + "Businesses/ConvenienceStore/store_stockdoor.png",
                Root + "Businesses/ConvenienceStore/Stockroom/stockroom_floor.png",
                Root + "Businesses/ConvenienceStore/Stockroom/stockroom_wall.png",
                Root + "Businesses/ConvenienceStore/Stockroom/stockroom_shelf.png",
                Root + "Businesses/ConvenienceStore/Stockroom/stockroom_box.png",
                Root + "Businesses/ConvenienceStore/Stockroom/stockroom_table.png",
                Root + "Businesses/Bar/bar_floor.png",
                Root + "Businesses/Bar/bar_wall.png",
                Root + "Businesses/Bar/bar_counter.png",
                Root + "Businesses/Bar/bar_backbar.png",
                Root + "Businesses/Bar/bar_stool.png",
                Root + "Businesses/Bar/bar_booth.png",
                Root + "Businesses/Bar/bar_bartender.png",
                Root + "Businesses/Bar/bar_neon.png",
                Root + "Businesses/Restaurant/restaurant_floor.png",
                Root + "Businesses/Restaurant/restaurant_wall.png",
                Root + "Businesses/Restaurant/restaurant_table.png",
                Root + "Businesses/Restaurant/restaurant_booth.png",
                Root + "Businesses/Restaurant/restaurant_counter.png",
                Root + "Businesses/Restaurant/restaurant_kitchen_door.png",
                Root + "Businesses/Restaurant/restaurant_waiter.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_floor.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_wall.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_counter.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_stove.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_sink.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_fridge.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_shelf.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_prep.png",
                Root + "Businesses/Restaurant/Kitchen/kitchen_chef.png",
                Root + "Businesses/AutoRepair/garage_floor.png",
                Root + "Businesses/AutoRepair/garage_wall.png",
                Root + "Businesses/AutoRepair/garage_lift.png",
                Root + "Businesses/AutoRepair/garage_car.png",
                Root + "Businesses/AutoRepair/garage_bench.png",
                Root + "Businesses/AutoRepair/garage_tires.png",
                Root + "Businesses/AutoRepair/garage_shelf.png",
                Root + "Businesses/AutoRepair/garage_mechanic.png",
                Root + "Businesses/Nightclub/club_floor.png",
                Root + "Businesses/Nightclub/club_wall.png",
                Root + "Businesses/Nightclub/club_dancefloor.png",
                Root + "Businesses/Nightclub/club_djbooth.png",
                Root + "Businesses/Nightclub/club_bar.png",
                Root + "Businesses/Nightclub/club_sofa.png",
                Root + "Businesses/Nightclub/club_speaker.png",
                Root + "Businesses/Nightclub/club_table.png",
                Root + "Businesses/Warehouse/warehouse_floor.png",
                Root + "Businesses/Warehouse/warehouse_wall.png",
                Root + "Businesses/Warehouse/warehouse_crate.png",
                Root + "Businesses/Warehouse/warehouse_pallet.png",
                Root + "Businesses/Warehouse/warehouse_shelf.png",
                Root + "Businesses/Warehouse/warehouse_forklift.png",
                Root + "Businesses/Warehouse/warehouse_loadingdoor.png",
                Root + "Businesses/Warehouse/warehouse_desk.png",
                Root + "Businesses/PortOffice/port_office_floor.png",
                Root + "Businesses/PortOffice/port_office_wall.png",
                Root + "Businesses/PortOffice/port_office_window.png",
                Root + "Businesses/PortOffice/port_office_desk.png",
                Root + "Businesses/PortOffice/port_office_cabinet.png",
                Root + "Businesses/PortOffice/port_office_map.png",
                Root + "Businesses/PortOffice/port_office_crate.png",
                Root + "Businesses/PortOffice/port_foreman.png"
            };

            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spritePixelsPerUnit != 32f ||
                    importer.filterMode != FilterMode.Point ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    !importer.alphaIsTransparency)
                {
                    Apply(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        private static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
