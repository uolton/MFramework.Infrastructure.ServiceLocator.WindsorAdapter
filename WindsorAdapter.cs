/*   Copyright 2009 - 2010 Marcus Bratton

     Licensed under the Apache License, Version 2.0 (the "License");
     you may not use this file except in compliance with the License.
     You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

     Unless required by applicable law or agreed to in writing, software
     distributed under the License is distributed on an "AS IS" BASIS,
     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     See the License for the specific language governing permissions and
     limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Facilities.FactorySupport;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Releasers;
using MFramework.Infrastructure.ServiceLocator.Exceptions;
using MFramework.Infrastructure.ServiceLocator.Resolution;
using MFramework.Infrastructure.ServiceLocator.ExtensionMethods;

namespace MFramework.Infrastructure.ServiceLocator.WindsorAdapter
{
    public class WindsorAdapter : IServiceLocatorAdapter
    {
        private readonly IKernel kernel;

        public WindsorAdapter() : this(new DefaultKernel())
        {
            this.kernel.ReleasePolicy = new NoTrackingReleasePolicy();
        }

        public WindsorAdapter(IKernel kernel)
        {
            this.kernel = kernel;
            if(!this.kernel.GetFacilities().OfType<FactorySupportFacility>().Any())
            {
            	this.kernel.AddFacility<FactorySupportFacility>();
            }
        }

        public void Dispose()
        {
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return (IEnumerable<object>)kernel.ResolveAll(serviceType);
        }

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            return kernel.ResolveAll<TService>();
        }

		public object GetInstance(Type type, string key, params IResolutionArgument[] parameters)
		{
			try
			{
				var args = new Dictionary<string, object>();

                var constructorParameters = parameters.OfType<ConstructorParameter, IResolutionArgument>();
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    var parameter = constructorParameters[i];
					args[parameter.Name] = parameter.Value;
				}

				return kernel.Resolve(key, type, args);
			}
			catch (ComponentNotFoundException ex)
			{
				throw new RegistrationNotFoundException(type, key, ex);
			}
		}

		public object GetInstance(Type type, params IResolutionArgument[] parameters)
		{
			try
			{
				var args = new Dictionary<string, object>();

                var constructorParameters = parameters.OfType<ConstructorParameter, IResolutionArgument>();
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    var parameter = constructorParameters[i];
					args[parameter.Name] = parameter.Value;
				}

				return kernel.Resolve(type, args);
			}
			catch (Exception ex)
			{
				throw new RegistrationNotFoundException(type, ex);
			}
		}

        public TService GetInstance<TService>(Type type, params IResolutionArgument[] arguments)
        {
            return (TService)GetInstance(type, arguments);
        }

        public TService GetInstance<TService>(string key, params IResolutionArgument[] arguments)
        {
            return (TService)GetInstance(typeof(TService), key, arguments);
        }

        public TService GetInstance<TService>(params IResolutionArgument[] arguments)
        {
            return (TService)GetInstance(typeof(TService), arguments);
        }

        public bool HasTypeRegistered(Type type)
        {
            return kernel.HasComponent(type);
        }

        public void Register(Type from, Type to)
        {
            if(kernel.HasComponent(from) || kernel.HasComponent(to)) return;
            kernel.Register(Component.For(from).ImplementedBy(to).LifeStyle.Transient.OnlyNewServices());
        }

        public void RegisterInstance(Type type, object instance)
        {
            RegisterInstanceWithName(type, instance, type.ToString());
        }

        public void RegisterWithName(Type from, Type to, string name)
        {
            if(kernel.HasComponent(name)) return;
            kernel.Register(Component.For(from).ImplementedBy(to).Named(name).LifeStyle.Transient);
        }

        public void RegisterInstanceWithName(Type type, object instance, string name)
        {
            if (kernel.HasComponent(name)) return;
            kernel.Register(Component.For(type).Instance(instance).Named(name).OnlyNewServices());
        }

        public void RegisterFactoryMethod(Type type, Func<object> func)
        {
            kernel.Register(Component.For(type).FactoryMethod(kernel, type, func).LifeStyle.Transient.OnlyNewServices());
        }
    }

   public static class ComponentRegistrationExtensions 
   {
       public static ComponentRegistration FactoryMethod(this ComponentRegistration reg, IKernel kernel, Type type, Func<object> factory)
       {
           Type item = typeof(InternalFactory<>).MakeGenericType(type);
           var factoryName = item.FullName;
           kernel.Register(Component.For(item).Named(factoryName).Instance(new GenericFactory(factory)).LifeStyle.Transient.OnlyNewServices());  
           reg.Configuration(Attrib.ForName("factoryId").Eq(factoryName), Attrib.ForName("factoryCreate").Eq("Create"));  
           return reg;  
       }  
     
       private class InternalFactory<T> : GenericFactory
       {
           public InternalFactory(Func<object> factoryMethod) : base(factoryMethod)
           {
           }
       }

       private class GenericFactory 
       {  
           private readonly Func<object> factoryMethod;  
     
           public GenericFactory(Func<object> factoryMethod) 
           {  
               this.factoryMethod = factoryMethod;  
           }  
     
           public object Create() 
           {  
               return factoryMethod();  
           }  
       }  
   }  
}