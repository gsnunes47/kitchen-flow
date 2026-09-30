using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class StructureOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "order_data",
                table: "orders",
                newName: "items");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Converte pedidos gravados pelo formato anterior ao domínio genérico de restaurante.
            migrationBuilder.Sql(
                """
                UPDATE orders
                SET phone = COALESCE(items ->> 'Phone', ''),
                    items = jsonb_build_array(
                        jsonb_build_object(
                            'name', items ->> 'Pizza',
                            'quantity', 1,
                            'price', CASE items ->> 'Pizza'
                                WHEN 'Calabresa' THEN 4500
                                WHEN 'Margherita' THEN 4200
                                WHEN 'Frango com catupiry' THEN 4800
                                WHEN 'Quatro queijos' THEN 5000
                                ELSE 0
                            END
                        ),
                        jsonb_build_object(
                            'name', items ->> 'Beverage',
                            'quantity', 1,
                            'price', CASE items ->> 'Beverage'
                                WHEN 'Coca-Cola' THEN 800
                                WHEN 'Guaraná' THEN 700
                                WHEN 'Água' THEN 500
                                ELSE 0
                            END
                        )
                    )
                WHERE jsonb_typeof(items) = 'object';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE orders
                SET items = jsonb_build_object(
                    'Phone', phone,
                    'Pizza', items -> 0 ->> 'name',
                    'Beverage', items -> 1 ->> 'name'
                )
                WHERE jsonb_typeof(items) = 'array';
                """);

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "items",
                table: "orders",
                newName: "order_data");
        }
    }
}
