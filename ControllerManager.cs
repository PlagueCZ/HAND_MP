using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rewired;
using Rewired.Data;
using UnityEngine;

using InputManager = Rewired.InputManager;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace HaND_MP
{
    internal struct GameController
    {
        public PlayerDeathApprenticeControllableController Assigned;
        public int Id;
        public ControllerType Type;
    }

    internal class ControllerManager
    {
        public static List<GameController> Controllers { get; private set; }

        public ControllerManager()
        {
            Controllers = new List<GameController>();

            ReInput.ControllerConnectedEvent += OnControllerConnect;
            ReInput.ControllerDisconnectedEvent += OnControllerDisconnect;
        }

        private void OnControllerConnect(ControllerStatusChangedEventArgs args)
        {
            Debug.Log($"Controller connected => ID: {args.controllerId}, Type: {args.controllerType}");

            PlayerSpawner.NumPlayers++;
            PlayerSpawner.SpawnPlayer();
        }

        private void OnControllerDisconnect(ControllerStatusChangedEventArgs args)
        {
            Debug.Log($"Controller disconnected => ID: {args.controllerId}, Type: {args.controllerType}");

            Controllers.Remove(Controllers.FirstOrDefault(controller =>
                controller.Id == args.controllerId && controller.Type == args.controllerType));

            PlayerSpawner.NumPlayers--;
        }
    }
}
