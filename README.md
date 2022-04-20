# Шаблон библиотеки

Шаблон проекта библиотки с настроенными workflows и уже готовой структурой.

Перед тем, как писать код, ознакомся с [правилами создания общей библиотеки](https://www.notion.so/mindbox/9019ea84fd4845a481127f6a8cb91b6c).

После создания новой библиотеки из шаблона нужно:
* Перейти в раздел settings\Manage access и по кнопке "Invite teams or people" добавить "Mindbox-moscow\developers" с правом write и "Mindbox-moscow\devops" с правом admin
* Заменить все вхождения LibraryTemplate на имя новой бибилотеки (включая имена файлов и `.github\workflows`)
* Запретить мержить бранчи, если не прошли статус чеки, всем включая администраторов ([пример](https://github.com/mindbox-moscow/Mindbox.Persistence.Rdbms/settings/branch_protection_rules/25072618))
* Заменить readme
* В новых проектах (если добавляются вручную) обязательно добавить Mindbox.Analyzers.V3
  
