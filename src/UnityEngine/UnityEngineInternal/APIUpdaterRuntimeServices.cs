using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngineInternal
{
	public sealed class APIUpdaterRuntimeServices
	{
#if !UNITY_EDITOR
		[Obsolete("Method is not meant to be used at runtime. Please, replace this call with GameObject.AddComponent<T>()/GameObject.AddComponent(Type).", true)]
		public static Component AddComponent(GameObject go, string sourceInfo, string name)
		{
			throw new Exception();
		}
#else

		[Obsolete(@"AddComponent(string) has been deprecated. Use GameObject.AddComponent<T>() / GameObject.AddComponent(Type) instead.
API Updater could not automatically update the original call to AddComponent(string name), because it was unable to resolve the type specified in parameter 'name'.
Instead, this call has been replaced with a call to APIUpdaterRuntimeServices.AddComponent() so you can try to test your game in the editor.
In order to be able to build the game, replace this call (APIUpdaterRuntimeServices.AddComponent()) with a call to GameObject.AddComponent<T>() / GameObject.AddComponent(Type).")]
		public static Component AddComponent(GameObject go, string sourceInfo, string name)
		{
			Debug.LogWarningFormat("Performing a potentially slow search for component {0}.", name);

			var type = ResolveType(name, Assembly.GetCallingAssembly(), sourceInfo);
			return type == null
				? null 
				: go.AddComponent(type);
		}

		private static Type ResolveType(string name, Assembly callingAssembly, string sourceInfo)
		{
			var foundOnUnityEngine = ComponentsFromUnityEngine.Where(n => n.Name == name).ToList();
			if (foundOnUnityEngine.Count == 1)
			{
				Debug.LogWarningFormat("[{1}] Type '{0}' found in UnityEngine, consider replacing with go.AddComponent<{0}>();", name, sourceInfo);
				return ComponentsFromUnityEngine.Single(n => n.Name == name);
			}

			var candidateType = callingAssembly.GetType(name);
			if (candidateType != null)
			{
				Debug.LogWarningFormat("[{1}] Component type '{0}' found on caller assembly. Consider replacing the call method call with: AddComponent<{0}>()", candidateType.FullName, sourceInfo);
				return candidateType;
			}

			candidateType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).SingleOrDefault(t => t.Name == name && typeof(Component).IsAssignableFrom(t));
			if (candidateType != null)
			{
				Debug.LogWarningFormat("[{2}] Component type '{0}' found on assembly {1}. Consider replacing the call method with: AddComponent<{0}>()", candidateType.FullName, candidateType.Assembly.Location, sourceInfo);
				return candidateType;
			}

			Debug.LogErrorFormat("[{1}] Component Type '{0}' not found.", name, sourceInfo);
			return null;
		}

		static APIUpdaterRuntimeServices()
		{
			var componentType = typeof (Component);
			ComponentsFromUnityEngine =  componentType.Assembly.GetTypes().Where(componentType.IsAssignableFrom).ToList();
		}
	
		private static IList<Type> ComponentsFromUnityEngine;
#endif
	}
}