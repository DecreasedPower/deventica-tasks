# Task 1
Спецификация к сервису: /Task1/specs.json<br/>
Схема БД:<br/>

Records
| Ограничения | Тип | Имя |
|-------------|-----|-----|
| PK | int | Id |
| | int | Code |
| | varchar(MAX) | Value |

Logs
| Ограничения | Тип | Имя |
|-------------|-----|-----|
| PK | int | Id |
| | datetime2 | Time |
| | varchar(MAX) | QueryString |
| | varchar(MAX) | RequestBody |
| | int | StatusCode |
| | varchar(MAX) | ResponseBody |
