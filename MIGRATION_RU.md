# 🚀 Migration Guide  

## Breaking changes  
* Все файлы, неймспейсы и ссылки изменены с `XCrew.Morpeh` на `Scellecs.Morpeh`.
* Все глобалы утратили метод `NextFrame`.  Вместо него теперь необходимо использовать метод `Publish`, который отправляет событие не в этот кадр, а в следующий как ранее делал `NextFrame`. Отправить событие в текущем кадре теперь невозможно, оно всегда отложенное. Это изменение связано с тем, что на всех проектах использовалась связка Publish + NextFrame, либо только NextFrame.
* `Entity.ID` теперь возвращает не `int`, а структуру типа `EntityID` в которой содержаться поля `id` и `gen`. Это необходимое изменение для работы с Jobs и Burst.
* `Filter.Length` удален полностью. Его заменила связка `Filter.GetLengthSlow()` и `Filter.IsEmpty()`. Это изменение связано с тем, что обновление кешированного значения длины для каждого фильтра давало большую нагрузку, а учитывая, что большинству фильтров не требовалось проверка на длину, то это была пустая нагрузка. Важный момент, что длина чаще всего использовалась для проверки на пустоту фильтра. Теперь получение длины дорогая операция, но проверка на пустоту такая же быстрая как и раньше. Это позволило сильно ускорить проекты с большим количеством фильтров.

## New API  
* IValidatable + IValidatableWithGameObject  
* .AsNative() для:
  * Archetype (NativeArchetype)
  * ComponentsCache (NativeCache)
  * FastList (NativeFastList)
  * IntFastList (NativeIntFastList)
  * Filter (NativeFilter)
  * IntHashMap (NativeIntHashMap)
  * World (NativeWorld)
* IMorpehLogger - интерфейс для кастомных логгеров (Console.WriteLine для окружений кроме Unity по дефолту)
* MORPEH_PROFILING - дефайн для автоматического профайлинга всех систем
* World.TryGetEntity(EntityId entityId, out Entity entity) - возвращает true и энтити если он существует, false и default(Entity) в противном случае
* entity.Dispose() - теперь публично доступное
* Отображение нескольких миров в World Browser
* Базовая поддержка TriInspector