-- list all tables
.mode table
SELECT name FROM sqlite_master WHERE type='table';
/*
+-----------------------+
|         name          |
+-----------------------+
| schema_migrations     |
| ar_internal_metadata  |
| articles              |
| sqlite_sequence       |
| categories            |
| authors               |
| authorships           |
| comments              |
| articles_categories   |
| __EFMigrationsLock    |
| __EFMigrationsHistory |
+-----------------------+
*/

-- schema of files table
PRAGMA table_info(articles);
/*
+-----+--------------+----------+---------+------------+----+
| cid |     name     |   type   | notnull | dflt_value | pk |
+-----+--------------+----------+---------+------------+----+
| 0   | id           | INTEGER  | 1       |            | 1  |
| 1   | title        | varchar  | 0       |            | 0  |
| 2   | content      | TEXT     | 0       |            | 0  |
| 3   | published_on | date     | 0       |            | 0  |
| 4   | created_at   | datetime | 1       |            | 0  |
| 5   | updated_at   | datetime | 1       |            | 0  |
| 6   | abstract     | TEXT     | 0       |            | 0  |
| 7   | url          | varchar  | 0       |            | 0  |
| 8   | shares       | INTEGER  | 0       |            | 0  |
+-----+--------------+----------+---------+------------+----+
*/

-- list all indexes
SELECT name FROM sqlite_master WHERE type='index';
/*
+------------------------------------------+
|                   name                   |
+------------------------------------------+
| sqlite_autoindex_schema_migrations_1     |
| sqlite_autoindex_ar_internal_metadata_1  |
| index_authorships_on_article_id          |
| index_authorships_on_author_id           |
| index_comments_on_article_id             |
| index_articles_categories_on_article_id  |
| index_articles_categories_on_category_id |
| sqlite_autoindex___EFMigrationsHistory_1 |
+------------------------------------------+
*/


-- schema of index_comments_on_article_id
PRAGMA index_info(index_comments_on_article_id);
/*
+-------+-----+------------+
| seqno | cid |    name    |
+-------+-----+------------+
| 0     | 6   | article_id |
+-------+-----+------------+
*/

-- count all records in `articles` table
SELECT count(id) as total_articles FROM articles;
/*
+----------------+
| total_articles |
+----------------+
| 929            |
+----------------+
*/

SELECT count(id) as total_comments FROM comments;
/*
+----------------+
| total_comments |
+----------------+
| 9930           |
+----------------+
*/

-- list all reccords in `categories` table
select * from categories;
/*
+----+------------------------------+----------------------------+----------------------------+
| id |            title             |         created_at         |         updated_at         |
+----+------------------------------+----------------------------+----------------------------+
| 1  | Sunday Review                | 2021-10-12 09:54:13.271261 | 2021-10-12 09:54:13.271261 |
| 2  | Health                       | 2021-10-12 09:54:13.737133 | 2021-10-12 09:54:13.737133 |
| 3  | Opinion                      | 2021-10-12 09:54:16.600003 | 2021-10-12 09:54:16.600003 |
| 4  | U.S.                         | 2021-10-12 09:54:19.283497 | 2021-10-12 09:54:19.283497 |
| 5  | World                        | 2021-10-12 09:54:21.930404 | 2021-10-12 09:54:21.930404 |
| 6  | Books                        | 2021-10-12 09:54:22.766255 | 2021-10-12 09:54:22.766255 |
| 7  | Science                      | 2021-10-12 09:54:22.989055 | 2021-10-12 09:54:22.989055 |
| 8  | Dining & Wine                | 2021-10-12 09:54:38.334931 | 2021-10-12 09:54:38.334931 |
| 9  | Business Day                 | 2021-10-12 09:54:47.553247 | 2021-10-12 09:54:47.553247 |
| 10 | Travel                       | 2021-10-12 09:54:54.198008 | 2021-10-12 09:54:54.198008 |
| 11 | Technology                   | 2021-10-12 09:55:05.393478 | 2021-10-12 09:55:05.393478 |
| 12 | Sports                       | 2021-10-12 09:55:06.555084 | 2021-10-12 09:55:06.555084 |
| 13 | Movies                       | 2021-10-12 09:55:17.602149 | 2021-10-12 09:55:17.602149 |
| 14 | Magazine                     | 2021-10-12 09:55:24.790364 | 2021-10-12 09:55:24.790364 |
| 15 | Home & Garden                | 2021-10-12 09:55:57.649171 | 2021-10-12 09:55:57.649171 |
| 16 | Arts                         | 2021-10-12 09:56:14.082073 | 2021-10-12 09:56:14.082073 |
| 17 | Fashion & Style              | 2021-10-12 09:57:06.251790 | 2021-10-12 09:57:06.251790 |
| 18 | Your Money                   | 2021-10-12 09:57:09.326117 | 2021-10-12 09:57:09.326117 |
| 19 | Education                    | 2021-10-12 09:57:10.845537 | 2021-10-12 09:57:10.845537 |
| 20 | N.Y. / Region                | 2021-10-12 09:57:24.699038 | 2021-10-12 09:57:24.699038 |
| 21 | Booming                      | 2021-10-12 09:57:56.846123 | 2021-10-12 09:57:56.846123 |
| 22 | Multimedia                   | 2021-10-12 10:00:37.905161 | 2021-10-12 10:00:37.905161 |
| 23 | Theater                      | 2021-10-12 10:03:52.385444 | 2021-10-12 10:03:52.385444 |
| 24 | Great Homes and Destinations | 2021-10-12 10:05:24.997398 | 2021-10-12 10:05:24.997398 |
| 25 | Automobiles                  | 2021-10-12 10:06:23.662931 | 2021-10-12 10:06:23.662931 |
| 26 | Style                        | 2021-10-12 10:09:49.975193 | 2021-10-12 10:09:49.975193 |
| 27 | T:Style                      | 2021-10-12 10:10:25.372551 | 2021-10-12 10:10:25.372551 |
| 28 | Real Estate                  | 2021-10-12 10:10:30.714473 | 2021-10-12 10:10:30.714473 |
| 29 |                              | 2021-10-12 10:13:16.111006 | 2021-10-12 10:13:16.111006 |
+----+------------------------------+----------------------------+----------------------------+
*/
