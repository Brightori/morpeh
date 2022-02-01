namespace Morpeh {
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Collections;
    using JetBrains.Annotations;
    using Unity.IL2CPP.CompilerServices;
    using UnityEngine;

#if UNITY_2019_1_OR_NEWER
    using morpeh.Core.NativeCollections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using UnityEngine.Profiling;
#endif

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class FilterExtensions {
        internal static void Dispose(this Filter filter) {
            foreach (var child in filter.childs) {
                child.Dispose();
            }

            filter.childs.Clear();
            filter.childs = null;

            if (filter.archetypes != null) {
                foreach (var archetype in filter.archetypes) {
                    archetype.RemoveFilter(filter);
                }

                filter.archetypes.Clear();
                filter.archetypes = null;
            }

            filter.includedTypeIds?.Clear();
            filter.includedTypeIds = null;
            filter.excludedTypeIds?.Clear();
            filter.excludedTypeIds = null;

            filter.Length = -1;

            filter.typeID = -1;
            filter.mode   = Filter.Mode.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateLength(this Filter filter) {
            filter.isDirty = false;
            filter.Length  = 0;
            foreach (var archetype in filter.archetypes) {
                filter.Length += archetype.length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FindArchetypes(this Filter filter, IntFastList newArchetypes) {
            var minLength = filter.includedTypeIds.length;
            foreach (var archId in newArchetypes) {
                var arch = filter.world.archetypes.data[archId];
                filter.CheckArchetype(arch, minLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FindArchetypes(this Filter filter) {
            var minLength = filter.includedTypeIds.length;
            foreach (var arch in filter.world.archetypes) {
                filter.CheckArchetype(arch, minLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckArchetype(this Filter filter, Archetype archetype, int minLength) {
            var typeIdsLength = archetype.typeIds.Length;
            if (typeIdsLength >= minLength) {
                var check = true;
                for (int i = 0, length = minLength; i < length; i++) {
                    var includedTypeId = filter.includedTypeIds.Get(i);
                    var foundInclude   = false;
                    for (int j = 0, lengthj = typeIdsLength; j < lengthj; j++) {
                        var typeId = archetype.typeIds[j];
                        if (typeId > includedTypeId) {
                            check = false;
                            goto BREAK;
                        }

                        if (typeId == includedTypeId) {
                            foundInclude = true;
                            break;
                        }
                    }

                    if (foundInclude == false) {
                        check = false;
                        goto BREAK;
                    }
                }

                for (int i = 0, length = filter.excludedTypeIds.length; i < length; i++) {
                    var excludedTypeId = filter.excludedTypeIds.Get(i);
                    for (int j = 0, lengthj = typeIdsLength; j < lengthj; j++) {
                        var typeId = archetype.typeIds[j];
                        if (typeId > excludedTypeId) {
                            break;
                        }

                        if (typeId == excludedTypeId) {
                            check = false;
                            goto BREAK;
                        }
                    }
                }

                BREAK:
                if (check) {
                    for (int i = 0, length = filter.archetypes.length; i < length; i++) {
                        if (filter.archetypes.data[i] == archetype) {
                            return;
                        }
                    }

                    filter.archetypes.Add(archetype);
                    archetype.AddFilter(filter);
                }
            }
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetEntity(this Filter filter, in int id) {
            var enumerator = filter.GetEnumerator();
            for (int i = 0, length = id + 1; i < length; i++) {
                if (enumerator.MoveNext() == false) {
                    throw new IndexOutOfRangeException();
                }
            }

            return enumerator.Current;
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity First(this Filter filter) {
            var enumerator = filter.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }

            throw new InvalidOperationException("The source sequence is empty.");
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity FirstOrDefault(this Filter filter) {
            var enumerator = filter.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }

            return default;
        }

        public static Filter With<T>(this Filter filter) where T : struct, IComponent
            => filter.CreateFilter<T>(Filter.Mode.Include);

        public static Filter Without<T>(this Filter filter) where T : struct, IComponent
            => filter.CreateFilter<T>(Filter.Mode.Exclude);

        private static Filter CreateFilter<T>(this Filter filter, Filter.Mode mode) where T : struct, IComponent {
            for (int i = 0, length = filter.childs.length; i < length; i++) {
                var child = filter.childs.data[i];
                if (child.mode == mode && child.typeID == TypeIdentifier<T>.info.id) {
                    return child;
                }
            }

            var newTypeId = TypeIdentifier<T>.info.id;

            IntFastList newIncludedTypeIds;
            IntFastList newExcludedTypeIds;
            if (filter.typeID == -1) {
                newIncludedTypeIds = new IntFastList();
                newExcludedTypeIds = new IntFastList();
            }
            else {
                newIncludedTypeIds = new IntFastList(filter.includedTypeIds);
                newExcludedTypeIds = new IntFastList(filter.excludedTypeIds);
            }

            if (mode == Filter.Mode.Include) {
                newIncludedTypeIds.Add(newTypeId);
            }
            else if (mode == Filter.Mode.Exclude) {
                newExcludedTypeIds.Add(newTypeId);
            }

            var newFilter = new Filter(filter.world, newTypeId, newIncludedTypeIds, newExcludedTypeIds, mode);
            filter.childs.Add(newFilter);

            return newFilter;
        }

#if UNITY_2019_1_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeComponentsGroup<TNative0> AsNative<TNative0>(this Filter filter)
            where TNative0 : unmanaged, IComponent {
            var nativeComponentsGroup = new NativeComponentsGroup<TNative0>();
            nativeComponentsGroup.length      = filter.Length;
            var nativeFilter = filter.AsNative();
            nativeComponentsGroup.components0 = new NativeComponents<TNative0>(nativeFilter, filter.world.GetCache<TNative0>().AsNative<TNative0>());
            return nativeComponentsGroup;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeComponentsGroup<TNative0, TNative1> AsNative<TNative0, TNative1>(this Filter filter)
            where TNative0 : unmanaged, IComponent
            where TNative1 : unmanaged, IComponent {
            var nativeComponentsGroup = new NativeComponentsGroup<TNative0, TNative1>();
            var nativeFilter          = filter.AsNative();
            nativeComponentsGroup.components0 = new NativeComponents<TNative0>(nativeFilter, filter.world.GetCache<TNative0>().AsNative<TNative0>());
            nativeComponentsGroup.components1 = new NativeComponents<TNative1>(nativeFilter, filter.world.GetCache<TNative1>().AsNative<TNative1>());
            return nativeComponentsGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeComponentsGroup<TNative0, TNative1, TNative2> AsNative<TNative0, TNative1, TNative2>(this Filter filter)
            where TNative0 : unmanaged, IComponent
            where TNative1 : unmanaged, IComponent
            where TNative2 : unmanaged, IComponent {
            var nativeComponentsGroup = new NativeComponentsGroup<TNative0, TNative1, TNative2>();
            var nativeFilter          = filter.AsNative();
            nativeComponentsGroup.components0 = new NativeComponents<TNative0>(nativeFilter, filter.world.GetCache<TNative0>().AsNative<TNative0>());
            nativeComponentsGroup.components1 = new NativeComponents<TNative1>(nativeFilter, filter.world.GetCache<TNative1>().AsNative<TNative1>());
            nativeComponentsGroup.components2 = new NativeComponents<TNative2>(nativeFilter, filter.world.GetCache<TNative2>().AsNative<TNative2>());
            return nativeComponentsGroup;
        }
#endif
    }
}