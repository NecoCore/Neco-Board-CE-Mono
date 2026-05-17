using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_accounts_owner_id",
                        column: x => x.owner_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tokens",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_tokens_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "columns",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    project_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    queue = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_columns", x => x.id);
                    table.ForeignKey(
                        name: "FK_columns_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    project_id = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    log_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_logs_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_logs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_project_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    project_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_project_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_project_roles_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_project_roles_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "column_tasks",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    column_id = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    text = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_column_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_column_tasks_accounts_owner_id",
                        column: x => x.owner_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_column_tasks_columns_column_id",
                        column: x => x.column_id,
                        principalTable: "columns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_attachments",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    task_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_attachments_column_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "column_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_images",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    task_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_images_column_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "column_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    task_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_users_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_users_column_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "column_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_login",
                table: "accounts",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_column_tasks_column_id",
                table: "column_tasks",
                column: "column_id");

            migrationBuilder.CreateIndex(
                name: "IX_column_tasks_owner_id",
                table: "column_tasks",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_columns_project_id",
                table: "columns",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_logs_project_id",
                table: "logs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_logs_user_id",
                table: "logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_owner_id",
                table: "projects",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_attachments_task_id",
                table: "task_attachments",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_images_task_id",
                table: "task_images",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_users_task_id",
                table: "task_users",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_task_users_user_id",
                table: "task_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tokens_AccountId",
                table: "tokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_user_project_roles_project_id",
                table: "user_project_roles",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_project_roles_user_id",
                table: "user_project_roles",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "task_attachments");

            migrationBuilder.DropTable(
                name: "task_images");

            migrationBuilder.DropTable(
                name: "task_users");

            migrationBuilder.DropTable(
                name: "tokens");

            migrationBuilder.DropTable(
                name: "user_project_roles");

            migrationBuilder.DropTable(
                name: "column_tasks");

            migrationBuilder.DropTable(
                name: "columns");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
