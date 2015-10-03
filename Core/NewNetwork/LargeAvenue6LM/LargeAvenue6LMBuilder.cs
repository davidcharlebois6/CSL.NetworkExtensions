﻿using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Globalization;
using CSL.NetworkExtensions.Framework;
using CSL.NetworkExtensions.Framework.ModParts;
using UnityEngine;

#if DEBUG
using Debug = CSL.NetworkExtensions.Framework.Debug;
#endif

namespace CSL.RoadExtensions.NewNetwork.LargeAvenue6LM
{
    public class LargeAvenue6LMBuilder : ActivablePart, INetInfoBuilder
    {
        public int OptionsPriority { get { return 25; } }
        public int Priority { get { return 25; } }

        public string TemplatePrefabName { get { return NetInfos.Vanilla.ROAD_6L; } }
        public string Name { get { return "Large Avenue M"; } }
        public string DisplayName { get { return "Six-Lane Road with Median"; } }
        public string CodeName { get { return "LARGEAVENUE_6LM"; } }
        public string Description { get { return "A six-lane road. Supports heavy traffic."; } }
        public string UICategory { get { return "RoadsLarge"; } }
        
        public string ThumbnailsPath    { get { return @"NewNetwork\LargeAvenue6LM\thumbnails.png"; } }
        public string InfoTooltipPath   { get { return @"NewNetwork\LargeAvenue6LM\infotooltip.png"; } }

        public NetInfoVersion SupportedVersions
        {
            get { return NetInfoVersion.Ground; }
        }

        public void BuildUp(NetInfo info, NetInfoVersion version)
        {
            ///////////////////////////
            // Template              //
            ///////////////////////////
            var largeRoadInfo = Prefabs.Find<NetInfo>(NetInfos.Vanilla.ROAD_6L);

            ///////////////////////////
            // 3DModeling            //
            ///////////////////////////
            if (version == NetInfoVersion.Ground)
            {

                var segments0 = info.m_segments[0];
                //var nodes0 = info.m_nodes[0];

                segments0.SetMeshes
                    (@"NewNetwork\LargeAvenue6LM\Meshes\Ground.obj");

                //nodes0.SetMeshes
                //    (@"NewNetwork\LargeAvenue6LM\Meshes\Ground_Node.obj");

                info.m_segments = new[] { segments0 };
                //info.m_nodes = new[] { nodes0 };
            }

            ///////////////////////////
            // Texturing             //
            ///////////////////////////
            switch (version)
            {
                case NetInfoVersion.Ground:
                    info.SetAllSegmentsTexture(
                        new TexturesSet
                           (@"NewNetwork\LargeAvenue6LM\Textures\Ground_Segment__MainTex.png",
                            @"NewNetwork\LargeAvenue6LM\Textures\Ground_Segment__AlphaMap.png"));
                    break;
            }

            ///////////////////////////
            // Set up                //
            ///////////////////////////
            info.m_class = largeRoadInfo.m_class.Clone(NetInfoClasses.NEXT_LARGE_ROAD);
            info.m_UnlockMilestone = largeRoadInfo.m_UnlockMilestone;
			info.m_hasParkingSpaces = false;

            // Setting up traffic lanes
            var vehicleLaneTypes = new[]
            {
                NetInfo.LaneType.Vehicle,
                NetInfo.LaneType.PublicTransport,
                NetInfo.LaneType.CargoVehicle,
                NetInfo.LaneType.TransportVehicle
            };

			var vehicleLanes = info.m_lanes
				.Where(l => vehicleLaneTypes.Contains(l.m_laneType))
				.OrderBy(l => l.m_position)
				.ToArray();

            var nonVehicleLanes = info.m_lanes
                .Where(l => !vehicleLaneTypes.Contains(l.m_laneType) && NetInfo.LaneType.Parking != l.m_laneType)
                .ToArray();

            info.m_lanes = vehicleLanes
                .Union(nonVehicleLanes)
                .ToArray();
            
            for (var i = 0; i < vehicleLanes.Length; i++)
			{
				var lane = vehicleLanes[i];

                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        if (lane.m_position < 0)
                        {
                            lane.m_position += -2.0f;
                        }
                        else
                        {
                            lane.m_position += 2.0f;
                        }
                        break;
                }
            }

            // Set up median lane
            var medianLanes = Prefabs.Find<NetInfo>(NetInfos.Vanilla.AVENUE_4L).m_lanes
                .Where(l => l.m_laneType == NetInfo.LaneType.None)
                .ToArray();

            medianLanes.First().m_laneProps.name = "Props - Large Middle";
            medianLanes.First().m_laneProps.m_props = medianLanes.First().m_laneProps.m_props
                .Where(p => p.m_prop.name != "Avenue Light")
                .ToArray();

            foreach (var p in medianLanes.First().m_laneProps.m_props)
            {
                if (p.m_position.x < 0)
                {
                    p.m_position.x += 1.4f;
                }
                else
                {
                    p.m_position.x -= 1.4f;
                }
            }

            info.m_lanes = info.m_lanes    // not sure if this is a good way to 'add' a new lane to the array..
                .Union(medianLanes)
                .ToArray();

            // info.Setup50LimitProps();

            // TODO: Replace leftlane traffic light to median
            // TODO: Use custom mesh (with median)

            if (version == NetInfoVersion.Ground)
            {
                var lrPlayerNetAI = largeRoadInfo.GetComponent<PlayerNetAI>();
                var playerNetAI = info.GetComponent<PlayerNetAI>();

                if (lrPlayerNetAI != null && playerNetAI != null)
                {
                    playerNetAI.m_constructionCost = lrPlayerNetAI.m_constructionCost * 9 / 10; // 10% decrease
                    playerNetAI.m_maintenanceCost = lrPlayerNetAI.m_maintenanceCost * 9 / 10; // 10% decrease
                }

                var lrRoadBaseAI = largeRoadInfo.GetComponent<RoadBaseAI>();
                var roadBaseAI = info.GetComponent<RoadBaseAI>();

                if (lrRoadBaseAI != null && roadBaseAI != null)
                {
                    roadBaseAI.m_noiseAccumulation = lrRoadBaseAI.m_noiseAccumulation;
                    roadBaseAI.m_noiseRadius = lrRoadBaseAI.m_noiseRadius;
                }
            }
        }

        public void ModifyExistingNetInfo()
        {
            var localizedStringsField = typeof(Locale).GetFieldByName("m_LocalizedStrings");
            var locale = SingletonLite<LocaleManager>.instance.GetLocale();
            var localizedStrings = (Dictionary<Locale.Key, string>)localizedStringsField.GetValue(locale);

            var kvp =
                localizedStrings
                .FirstOrDefault(kvpInternal =>
                    kvpInternal.Key.m_Identifier == "NET_TITLE" &&
                    kvpInternal.Key.m_Key == NetInfos.Vanilla.ROAD_6L);

            if (!Equals(kvp, default(KeyValuePair<Locale.Key, string>)))
            {
                localizedStrings[kvp.Key] = "Six-Lane Road with Median";
            }
        }
    }
}
