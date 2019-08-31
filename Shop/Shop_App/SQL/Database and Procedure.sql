-- Create a new database
CREATE DATABASE Shop

GO

-- Use the new database after it is created
USE Shop

-- Create a table
CREATE TABLE Products (
	Id INT PRIMARY KEY IDENTITY,
	Name NVARCHAR(100) NOT NULL,
	Likes INT,
	Dislikes INT
)

GO -- Create procedure must be the only statement in the batch

CREATE PROC usp_InsertProduct(@productName NVARCHAR(100), @likes INT, @dislikes INT)
AS
BEGIN
	INSERT INTO Products(Name, Likes, Dislikes) VALUES 
		(@productName, @likes, @dislikes)
END

-- Test the procedure
EXEC usp_InsertProduct @productName = 'first product', @likes = 13, @dislikes = 6

SELECT * FROM Products