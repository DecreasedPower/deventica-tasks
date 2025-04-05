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

# Task 2
1. Написать запрос, который возвращает наименование клиентов и кол-во контактов клиентов
```
SELECT c.ClientName, COUNT(cc.ContactValue) as ContactCount
FROM Clients c
JOIN ClientContacts cc ON c.Id = cc.ClientId
GROUP BY c.ClientName
```

2. Написать запрос, который возвращает список клиентов, у которых есть более 2 контактов
```
SELECT ClientId
FROM ClientContacts
GROUP BY ClientId
HAVING COUNT(ContactValue) > 2
```
