using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CryptInject.EntityFrameworkExample
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext() : base("Data Source=(LocalDb)\\v11.0;Initial Catalog=TestData;Integrated Security=True")
        {
        }

        public DbSet<Patient> Patients { get; set; }

        // The below method is required to build links between the entities specified by the developer and the generated proxies.
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var modelConfig = modelBuilder.GetType()
                .GetProperty("ModelConfiguration", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(modelBuilder);
            var entities = modelConfig.GetType().GetProperty("Entities", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(modelConfig) as List<Type>;

            if (entities != null)
            {
                foreach (var type in entities)
                {
                    var efEntityProxy = type.GetEncryptedType() ?? Activator.CreateInstance(type).AsEncrypted().GetType();

                    var emcType = typeof (EntityMappingConfiguration<>).GetGenericTypeDefinition();
                    var genericEmcType = emcType.MakeGenericType(efEntityProxy);
                    var emcObj = Activator.CreateInstance(genericEmcType);
                    foreach (
                        var prop in
                            efEntityProxy.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p => p.GetCustomAttribute<EncryptableAttribute>() != null))
                    {
                        var p = Expression.Parameter(efEntityProxy);
                        var expr = Expression.Lambda(Expression.PropertyOrField(p, prop.Name), p);

                        var targetMethod =
                            emcObj.GetType()
                                .GetMethods()
                                .FirstOrDefault(m => m.ToString().EndsWith(prop.PropertyType.FullName + "]])"));
                        if (targetMethod != null)
                        {
                            targetMethod.Invoke(emcObj, new object[] {expr});
                        }
                        else
                        {
                            targetMethod =
                                emcObj.GetType().GetMethods().FirstOrDefault(m => m.ToString().EndsWith("T]])"));
                            var targetMethodGen = targetMethod.MakeGenericMethod(prop.PropertyType);
                            targetMethodGen.Invoke(emcObj, new object[] {expr});
                        }
                    }

                    modelBuilder.RegisterEntityType(efEntityProxy);
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
