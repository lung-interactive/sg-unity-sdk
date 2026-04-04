using System;
using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.UseCases;


namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Provides dependency resolution for Editor services, repositories,
    /// use cases, and controllers. Uses reflection and a type-instance
    /// dictionary for lazy, decoupled management.
    /// </summary>
    public class EditorServiceProvider
    {
        #region Singleton

        private static EditorServiceProvider _instance;

        /// <summary>
        /// Gets the singleton instance of the provider.
        /// </summary>
        public static EditorServiceProvider Instance =>
            _instance ??= new EditorServiceProvider();

        #endregion

        #region Service Registry

        private readonly Dictionary<Type, object> _services = new();

        private EditorServiceProvider()
        {
            // Eagerly register commonly used editor use-cases so callers
            // receive singleton instances and don't rely solely on
            // convention-based discovery.
            Register(new GenerateBuildsUseCase(GetService<IBuildGenerationService>()));
        }

        /// <summary>
        /// Registers a custom implementation for a service, repository,
        /// use case, or controller.
        /// </summary>
        /// <typeparam name="T">Type to register.</typeparam>
        /// <param name="instance">Instance to register.</param>
        public void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        #endregion

        #region Resolution

        /// <summary>
        /// Gets or creates a service, repository, use case, or controller
        /// of the specified type. Uses convention-based resolution.
        /// </summary>
        /// <typeparam name="T">Type to resolve.</typeparam>
        /// <returns>Instance of the requested type.</returns>
        public T GetService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var instance))
                return (T)instance;

            var implType = FindImplementationType(type);
            if (implType == null)
            {
                throw new InvalidOperationException(
                    $"No implementation found for {type.FullName}");
            }

            // Resolve constructor dependencies recursively
            var ctor = implType.GetConstructors()[0];
            var parameters = ctor.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var method = typeof(EditorServiceProvider)
                    .GetMethod("GetService")
                    .MakeGenericMethod(paramType);
                args[i] = method.Invoke(Instance, null);
            }
            T created = ctor.Invoke(args) as T;
            Register(created);
            return created;
        }

        #endregion

        #region Convention-based Implementation Discovery

        /// <summary>
        /// Finds a concrete implementation for the given interface type
        /// using naming conventions (Repository, Service, UseCase, Controller).
        /// </summary>
        /// <param name="interfaceType">Interface or abstract type.</param>
        /// <returns>Concrete implementation type or null.</returns>
        private static Type FindImplementationType(Type interfaceType)
        {
            var assembly = interfaceType.Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (interfaceType.IsAssignableFrom(type)
                    && type.IsClass && !type.IsAbstract)
                {
                    // Convention: prefer types ending with
                    // 'Repository', 'Service', 'UseCase', 'Controller', 'ViewModel'
                    if (type.Name.EndsWith("Repository")
                        || type.Name.EndsWith("Service")
                        || type.Name.EndsWith("UseCase")
                        || type.Name.EndsWith("Controller")
                        || type.Name.EndsWith("ViewModel"))
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
