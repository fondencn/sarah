using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sarah.DeviceService.Model.Animations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SceneNameAttribute : Attribute
    {
        public SceneNameAttribute(string name) : base()
        {
            this.Name = name;
        }
        public string Name { get; set; }
    }


    public abstract class Scene
    {
        protected readonly IDeviceService _deviceService;

        protected abstract void Start();
        protected abstract void Stop();


        public Scene(IDeviceService deviceService) 
        {
            _deviceService = deviceService;
        }

        private readonly static Dictionary<Type, Scene> _activeScenes = new Dictionary<Type, Scene>();
        public static IEnumerable<Type> KnownScenes { get; } = LoadSceneDefinitions();

        private static IEnumerable<Type> LoadSceneDefinitions()
        {
            return Assembly.GetAssembly(typeof(Scene)).GetTypes().Where(item => item.BaseType == typeof(Scene));
        }

        public static bool IsSceneActive(Type sceneType)
        {
            return _activeScenes.ContainsKey(sceneType);
        }

        public static void Start(Type sceneType)
        {
            if(sceneType == null)
            {
                throw new ArgumentNullException(nameof(sceneType));
            }

            if (!_activeScenes.ContainsKey(sceneType))
            {
                Scene scene = Activator.CreateInstance(sceneType) as Scene;
                scene.Start();
                _activeScenes.Add(sceneType, scene);
            }
        }

        public static void Stop(Type sceneType)
        {
            if (sceneType == null)
            {
                throw new ArgumentNullException(nameof(sceneType));
            }

            if (_activeScenes.ContainsKey(sceneType))
            {
                Scene scene = _activeScenes[sceneType];
                scene.Stop();
                _activeScenes.Remove(sceneType);
            }
        }

        public static void Start(string sceneTypeName) => Start(FindScene(sceneTypeName));
        public static void Stop(string sceneTypeName) => Stop(FindScene(sceneTypeName));

        private static Type FindScene(string sceneTypeName)
        {
            // Console.WriteLine("FindScene " + (sceneTypeName ?? "(null)"));
            string cleanedSceneName = sceneTypeName.Trim(',', ' ', '.');
            Type sceneType = KnownScenes.FirstOrDefault(item => String.Equals(item.Name, sceneTypeName, StringComparison.OrdinalIgnoreCase));
            if(sceneType == null)
            {
                sceneType = KnownScenes.FirstOrDefault(item => item.GetCustomAttributes<SceneNameAttribute>()
                    ?.Any(attr => String.Equals(attr.Name, sceneTypeName, StringComparison.OrdinalIgnoreCase)) == true);
            }

            return sceneType;
        }

    }
}
