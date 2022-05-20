using BepInEx;
using BepInEx.Logging;
using Rewired;
using Rewired.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HAND_MP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("HaveaNiceDeath.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private int _newControllerId;
        private ControllerType _newControllerType;
        private bool _player2Active;
        private static GameObject _player2;

        private void Awake()
        {
            ReInput.ControllerConnectedEvent += OnControllerConnect;
            ReInput.ControllerDisconnectedEvent += OnControllerDisconnect;
            SceneManager.sceneLoaded += OnSceneLoad;
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(WaitForRewired());
        }

        private void OnControllerConnect(ControllerStatusChangedEventArgs args)
        {
            Log("Controller connected: " + args.controllerId + ", " + args.controllerType);
            _newControllerId = args.controllerId;
            _newControllerType = args.controllerType;
            _player2Active = true;

            if (_player2 == null)
                StartCoroutine(CreatePlayer(args.controllerId, args.controllerType));
        }

        private void OnControllerDisconnect(ControllerStatusChangedEventArgs args)
        {
            Log("Controller disconnected");
            _player2Active = false;

            var mainCam = GameObject.Find("MainCamera");
            var classicCam = mainCam.GetComponentInChildren<ClassicPlayerCamera>(true);
            var targetControllers = typeof(ClassicPlayerCamera).GetField("targetControllers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(classicCam) as List<UserControllableSimple>;

            Destroy(GameObject.Find("SecondaryCamera"));

            if (_player2 != null)
                Destroy(_player2);
        }

        private string[] _nonGameplayScenes = new string[] 
        {
            "LoadSplash",
            "Splash",
            "Tool",
        };
        private Coroutine _createPlayerRoutine;
        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            Log("Loaded scene: " + scene.name);
            if (_createPlayerRoutine == null && _player2 == null && !_nonGameplayScenes.Contains(scene.name) && _player2Active)
            {
                Log("Creating player 2...");
                _createPlayerRoutine = StartCoroutine(CreatePlayer(_newControllerId, _newControllerType));
            }
        }

        private IEnumerator CreatePlayer(int newControllerId, ControllerType controllerType)
        {
            // Cleanup
            var spawnedPlayers = typeof(PlayerSpawner).GetField("spawnedPlayers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as List<UserControllableSimple>;
            spawnedPlayers.ForEach(player => { if (player.GetComponentInChildren<PlayerDeathApprenticeControllableController>(true).PlayerInput.id > 0) spawnedPlayers.Remove(player); });
            typeof(PlayerSpawner).GetField("spawnedPlayers", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, spawnedPlayers);

            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Player").Count() > 0);
            var spawner = FindObjectsOfType<ObjectSpawnOnStart>(true).FirstOrDefault(spawn => spawn.name.Contains("Player"));
            spawner.DebugSpawn();
            
            int playerId = 0;

            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Player").Where(go => !go.name.Contains("Physic")).Count() > 1);

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player").Where(go => !go.name.Contains("Physic")).ToArray();

            PlayerDeathApprenticeControllableController ctrl;
            foreach (var player in players)
            {
                player.name = "Player " + playerId;
                ctrl = player.GetComponentInChildren<PlayerDeathApprenticeControllableController>(true);
                ctrl.inputHandler.controller = player.GetComponent<MagicCustomController>();
                typeof(UserControllable).GetProperty("PlayerInput", BindingFlags.Instance | BindingFlags.Public).SetValue(ctrl, ReInput.players.GetPlayer(playerId));
                typeof(UserControllableSimple).GetMethod("AddPlayer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ctrl, new object[] { });
                playerId++;
            }

            _player2 = players[1];
            _player2.GetComponentInChildren<ClassicHealthManager>(true).ResetLife();
            ctrl = players[1].GetComponentInChildren<PlayerDeathApprenticeControllableController>(true);
            ctrl.PlayerInput.controllers.ClearAllControllers();
            ctrl.PlayerInput.controllers.AddController(controllerType, newControllerId, true);

            Physics2D.IgnoreCollision(
                players[0].transform.Find("RotationHandler/Colliders/Repulsor").GetComponent<BoxCollider2D>(),
                players[1].transform.Find("RotationHandler/Colliders/Repulsor").GetComponent<BoxCollider2D>());

            int countLoaded = SceneManager.sceneCount;
            Scene[] loadedScenes = new Scene[countLoaded];
            for (int sceneIndex = 0; sceneIndex < countLoaded; sceneIndex++)
                loadedScenes[sceneIndex] = SceneManager.GetSceneAt(sceneIndex);

            if (!loadedScenes.Contains(SceneManager.GetSceneByName("Lobby_2")))
            {
                yield return new WaitUntil(() => ctrl.Statemachine.IsStateActive("Wait"));

                // Fix transition from elevators
                ctrl.Statemachine.TransitionTo("NormalMovement", null, true);
                
                if (!loadedScenes.Contains(SceneManager.GetSceneByName("Lobby_2")))
                {
                    yield return new WaitUntil(() => !players[1].transform.Find("RotationHandler/Graphics/Final/FinalGraph/SpriteRender").gameObject.activeSelf);
                    players[1].transform.Find("RotationHandler/Graphics/Final/FinalGraph/SpriteRender").gameObject.SetActive(true);
                }
            }

            yield return StartCoroutine(WaitForCamera(players[0].GetComponentInChildren<PlayerDeathApprenticeControllableController>(true), ctrl));
        }

        private IEnumerator WaitForRewired()
        {
            yield return new WaitUntil(() => GameObject.Find("AllManagers/RewiredInputManager/EventHandler") != null);

            var rewired = GameObject.Find("AllManagers/RewiredInputManager");
            var manager = rewired.GetComponent<Rewired.InputManager>();
            var userData = (UserData)typeof(InputManager_Base).GetField("_userData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            userData.DuplicatePlayer(1);
            typeof(InputManager_Base).GetField("_userData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, userData);
            typeof(ReInput).GetField("LWLODUkCwfcNvCWHWwdHlYbcikq", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, userData);
            var method = typeof(InputManager_Base).GetMethod("yvZAtTkpGHfYacGujfIUnfoxsxvA", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<ConfigVars, object>), manager) as Func<ConfigVars, object>;
            var dataFiles = typeof(InputManager_Base).GetField("_controllerDataFiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager) as ControllerDataFiles;
            typeof(ReInput).GetMethod("EJpmrTgGvrhKjJnkpXbomYBpQTQ", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { manager, method, userData.ConfigVars, dataFiles, userData });
        }

        private IEnumerator WaitForCamera(UserControllableSimple player1, UserControllableSimple player2)
        {
            yield return new WaitUntil(() => GameObject.Find("MainCamera") != null);

            var mainCam = GameObject.Find("MainCamera");
            mainCam.GetComponentsInChildren<Camera>(true).ToList().ForEach(cam => cam.targetDisplay = 0);
            var classicCam = mainCam.GetComponentInChildren<ClassicPlayerCamera>(true);
            classicCam.GetPlayersInUpdate = false;
            var targetControllers = typeof(ClassicPlayerCamera).GetField("targetControllers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(classicCam) as List<UserControllableSimple>;
            targetControllers = new() { player1 };
            typeof(ClassicPlayerCamera).GetField("targetControllers", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(classicCam, targetControllers);

            var secondaryCam = GameObject.Find("SecondaryCamera");
            if (secondaryCam == null)
            {
                Display.displays[1].Activate();

                secondaryCam = Instantiate(mainCam, mainCam.transform.parent);
                secondaryCam.GetComponentsInChildren<Camera>(true).ToList().ForEach(cam => cam.targetDisplay = 1);
                secondaryCam.transform.Find("GlobalArtSetting").gameObject.SetActive(false);
                secondaryCam.name = "SecondaryCamera";
                DontDestroyOnLoad(secondaryCam);
                Destroy(secondaryCam.GetComponentInChildren<AudioListener>(true));
            }

            classicCam = secondaryCam.GetComponentInChildren<ClassicPlayerCamera>(true);
            classicCam.GetPlayersInUpdate = false;
            targetControllers = typeof(ClassicPlayerCamera).GetField("targetControllers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(classicCam) as List<UserControllableSimple>;
            targetControllers = new() { player2 };
            typeof(ClassicPlayerCamera).GetField("targetControllers", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(classicCam, targetControllers);

            // Re-enable post-processing
            GameObject setting = mainCam.transform.Find("GlobalArtSetting").gameObject;
            setting?.SetActive(false);
            yield return new WaitForEndOfFrame();
            setting?.SetActive(true);

            setting = secondaryCam.transform.Find("GlobalArtSetting").gameObject;
            setting?.SetActive(false);
            yield return new WaitForEndOfFrame();
            setting?.SetActive(true);

            _createPlayerRoutine = null;
        }

        private void Log(object msg)
        {
            Logger.Log(LogLevel.All, msg);
        }

        private void OnDestroy()
        {
            ReInput.ControllerConnectedEvent -= OnControllerConnect;
            ReInput.ControllerDisconnectedEvent -= OnControllerDisconnect;
            SceneManager.sceneLoaded -= OnSceneLoad;
        }
    }
}
