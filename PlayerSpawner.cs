using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rewired;
using Rewired.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace HaND_MP
{
    internal class PlayerSpawner
    {
        private static readonly string[] NonGameplayScenes =
        {
            "LoadSplash",
            "Splash",
            "Tool",
        };

        public static int NumPlayers { get; set; } = 0;

        public static void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            Debug.Log($"Spawning {NumPlayers - 1} players");

            if (NonGameplayScenes.Contains(scene.name) || loadSceneMode != LoadSceneMode.Single) return;

            for (int playerNum = 0; playerNum < NumPlayers - 1; playerNum++)
            {
                Debug.Log("Spawning player " + playerNum);
                SpawnPlayer();
            }
        }

        public static PlayerDeathApprenticeControllableController SpawnPlayer()
        {
            foreach (var spawn in UObject.FindObjectsOfType<ObjectSpawnOnStart>())
            {
                foreach (var asset in spawn.ToSpawn)
                {
                    Debug.Log($"{spawn.name} spawns {asset.AssetName}");
                }
            }

            var spawner = UObject.FindObjectsOfType<ObjectSpawnOnStart>().FirstOrDefault();

            Debug.Log("Spawner null? " + (spawner == null));
            if (spawner == null) return null;

            spawner.DebugSpawn();

            var players = GameObject.FindGameObjectsWithTag("Player")
                .Where(gameObject => !gameObject.name.Contains("Physic")).ToArray();

            PlayerDeathApprenticeControllableController controller = null;
            int playerId = 0;
            foreach (var player in players)
            {
                player.name = "Player " + playerId;
                controller = player.GetComponentInChildren<PlayerDeathApprenticeControllableController>(true);
                controller.inputHandler.controller = player.GetComponent<MagicCustomController>();
                typeof(UserControllable).GetProperty("PlayerInput", BindingFlags.Instance | BindingFlags.Public)
                    ?.SetValue(controller, ReInput.players.GetPlayer(playerId));
                typeof(UserControllableSimple).GetMethod("AddPlayer", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller, new object[] { });
                playerId++;
            }

            var firstPlayer = players[0];
            var lastPlayer = players[players.Length - 1];
            lastPlayer.transform.position = firstPlayer.transform.position;
            lastPlayer.SetActive(true);

            var rewired = GameObject.Find("AllManagers/RewiredInputManager");
            var manager = rewired.GetComponent<InputManager>();
            var userData = (UserData)typeof(InputManager_Base)
                .GetField("_userData", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(manager);

            userData?.DuplicatePlayer(playerId);

            typeof(InputManager_Base).GetField("_userData", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, userData);

            typeof(ReInput).GetField("LWLODUkCwfcNvCWHWwdHlYbcikq", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, userData);

            var method =
                typeof(InputManager_Base)
                    .GetMethod("yvZAtTkpGHfYacGujfIUnfoxsxvA", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.CreateDelegate(typeof(Func<ConfigVars, object>), manager) as Func<ConfigVars, object>;

            var dataFiles =
                typeof(InputManager_Base)
                    .GetField("_controllerDataFiles", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(manager) as ControllerDataFiles;

            typeof(ReInput).GetMethod("EJpmrTgGvrhKjJnkpXbomYBpQTQ", BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { manager, method, userData?.ConfigVars, dataFiles, userData });

            return controller;
        }
    }
}
