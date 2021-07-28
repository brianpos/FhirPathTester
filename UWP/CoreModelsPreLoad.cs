extern alias dstu2;
extern alias stu3;
extern alias r4;

using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FhirPathTesterUWP
{
    public class CoreModelsPreLoad
    {
        public static Task Preload(Action<string> UpdateStatus)
        {
            // in the background load up the 3 sets of ClassLibraries
            UpdateStatus("Pre-loading the R4 classes...");
            var inspectorR4 = r4.Hl7.Fhir.Serialization.BaseFhirParser.Inspector;
            inspectorR4.Import(typeof(r4.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
            var pm4r = inspectorR4.GetType().GetField("_resourceClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _resourceClasses = pm4r.GetValue(inspectorR4) as Dictionary<Tuple<string, string>, r4.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _resourceClasses.Values)
            {
                inspectR4Properties(cm);
            }
            var pm4t = inspectorR4.GetType().GetField("_dataTypeClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _dataTypeClasses = pm4t.GetValue(inspectorR4) as Dictionary<string, r4.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _dataTypeClasses.Values)
            {
                inspectR4Properties(cm);
            }

            // stu3.Hl7.Fhir.Serialization.BaseFhirParser.Inspector.Import(typeof(stu3.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
            UpdateStatus("Pre-loading the STU3 classes...");
            var i3field = typeof(stu3.Hl7.Fhir.Serialization.BaseFhirParser).GetProperty("Inspector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var inspectorR3 = i3field.GetValue(null) as stu3.Hl7.Fhir.Introspection.ModelInspector; // stu3.Hl7.Fhir.Serialization.BaseFhirParser.Inspector;
            inspectorR3.Import(typeof(dstu2.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
            var pm3r = inspectorR3.GetType().GetField("_resourceClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _resourceClassesR3 = pm3r.GetValue(inspectorR3) as Dictionary<Tuple<string, string>, stu3.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _resourceClassesR3.Values)
            {
                inspectSTU3Properties(cm);
            }
            var pm3t = inspectorR3.GetType().GetField("_dataTypeClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _dataTypeClassesR3 = pm3t.GetValue(inspectorR3) as Dictionary<string, stu3.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _dataTypeClassesR3.Values)
            {
                inspectSTU3Properties(cm);
            }

            UpdateStatus("Pre-loading the DSTU2 classes...");
            var inspectorR2 = dstu2.Hl7.Fhir.Serialization.BaseFhirParser.Inspector;
            inspectorR2.Import(typeof(dstu2.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
            var pm2r = inspectorR2.GetType().GetField("_resourceClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _resourceClassesR2 = pm2r.GetValue(inspectorR2) as Dictionary<Tuple<string, string>, dstu2.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _resourceClassesR2.Values)
            {
                inspectDSTU2Properties(cm);
            }

            var pm2t = inspectorR2.GetType().GetField("_dataTypeClasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var _dataTypeClassesR2 = pm2t.GetValue(inspectorR2) as Dictionary<string, dstu2.Hl7.Fhir.Introspection.ClassMapping>;
            foreach (var cm in _dataTypeClassesR2.Values)
            {
                inspectDSTU2Properties(cm);
            }

            UpdateStatus("Pre-loading complete");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enumerate this class' properties using reflection, create PropertyMappings
        /// for them and add them to the PropertyMappings.
        /// </summary>
        private static void inspectR4Properties(r4.Hl7.Fhir.Introspection.ClassMapping me)
        {
            foreach (var property in ReflectionHelper.FindPublicProperties(me.NativeType))
            {
                // Skip properties that are marked as NotMapped
                if (ReflectionHelper.GetAttribute<Hl7.Fhir.Introspection.NotMappedAttribute>(property) != null) continue;
                var propMapping = r4.Hl7.Fhir.Introspection.PropertyMapping.Create(property);

                var existingPopMapping = me.FindMappedElementByName(propMapping.Name);

                // patch the getter/setter
                Func<object, object> getter = instance => property.GetValue(instance, null);
                Action<object, object> setter = (instance, value) => property.SetValue(instance, value, null);

                if (r4Getter == null)
                    r4Getter = existingPopMapping.GetType().GetField("_getter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (r4Setter == null)
                    r4Setter = existingPopMapping.GetType().GetField("_setter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                r4Getter.SetValue(existingPopMapping, getter);
                r4Setter.SetValue(existingPopMapping, setter);
                //propMapping._getter = getter;
                //propMapping._setter = setter;
            }
        }
        private static FieldInfo r4Getter;
        private static FieldInfo r4Setter;

        /// <summary>
        /// Enumerate this class' properties using reflection, create PropertyMappings
        /// for them and add them to the PropertyMappings.
        /// </summary>
        private static void inspectSTU3Properties(stu3.Hl7.Fhir.Introspection.ClassMapping me)
        {
            foreach (var property in ReflectionHelper.FindPublicProperties(me.NativeType))
            {
                // Skip properties that are marked as NotMapped
                if (ReflectionHelper.GetAttribute<Hl7.Fhir.Introspection.NotMappedAttribute>(property) != null) continue;
                var propMapping = stu3.Hl7.Fhir.Introspection.PropertyMapping.Create(property);

                var existingPopMapping = me.FindMappedElementByName(propMapping.Name);

                // patch the getter/setter
                Func<object, object> getter = instance => property.GetValue(instance, null);
                Action<object, object> setter = (instance, value) => property.SetValue(instance, value, null);

                if (stu3Getter == null)
                    stu3Getter = existingPopMapping.GetType().GetField("_getter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stu3Setter == null)
                    stu3Setter = existingPopMapping.GetType().GetField("_setter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                stu3Getter.SetValue(existingPopMapping, getter);
                stu3Setter.SetValue(existingPopMapping, setter);
                //propMapping._getter = getter;
                //propMapping._setter = setter;
            }
        }
        private static FieldInfo stu3Getter;
        private static FieldInfo stu3Setter;


        /// <summary>
        /// Enumerate this class' properties using reflection, create PropertyMappings
        /// for them and add them to the PropertyMappings.
        /// </summary>
        private static void inspectDSTU2Properties(dstu2.Hl7.Fhir.Introspection.ClassMapping me)
        {
            foreach (var property in ReflectionHelper.FindPublicProperties(me.NativeType))
            {
                // Skip properties that are marked as NotMapped
                if (ReflectionHelper.GetAttribute<Hl7.Fhir.Introspection.NotMappedAttribute>(property) != null) continue;
                var propMapping = dstu2.Hl7.Fhir.Introspection.PropertyMapping.Create(property);

                var existingPopMapping = me.FindMappedElementByName(propMapping.Name);

                // patch the getter/setter
                Func<object, object> getter = instance => property.GetValue(instance, null);
                Action<object, object> setter = (instance, value) => property.SetValue(instance, value, null);

                if (dstu2Getter == null)
                    dstu2Getter = existingPopMapping.GetType().GetField("_getter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dstu2Setter == null)
                    dstu2Setter = existingPopMapping.GetType().GetField("_setter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                dstu2Getter.SetValue(existingPopMapping, getter);
                dstu2Setter.SetValue(existingPopMapping, setter);
                //propMapping._getter = getter;
                //propMapping._setter = setter;
            }
        }
        private static FieldInfo dstu2Getter;
        private static FieldInfo dstu2Setter;
    }
}
