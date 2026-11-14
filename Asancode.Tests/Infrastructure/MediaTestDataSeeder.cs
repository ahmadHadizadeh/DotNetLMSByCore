using DataLayer.Context;
using DataLayer.Course;
using DataLayer.Entities.Course;
using DataLayer.Entities.Permission;
using DataLayer.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Asancode.Tests.Infrastructure;

public sealed record MediaTeacherCourseScenario(
    string OwnerUserName,
    string OtherTeacherUserName,
    int CourseId);

public static class MediaTestDataSeeder
{
    public static async Task<MediaTeacherCourseScenario> SeedTeacherCourseAsync(
        AsanwebContext dbContext,
        CancellationToken cancellationToken)
    {
        var uniqueKey = Guid.NewGuid().ToString("N");
        var owner = CreateUser($"owner-{uniqueKey}");
        var otherTeacher = CreateUser($"other-{uniqueKey}");

        dbContext.Users.AddRange(owner, otherTeacher);

        var role = new Roles
        {
            RolesTitles = $"Media test teacher {uniqueKey}",
            IsDelete = false
        };
        dbContext.Roles.Add(role);

        await dbContext.SaveChangesAsync(cancellationToken);

        // TeacherController is protected by PermissionChecker(2).
        // Seed permission ID 2 explicitly through the normal identity sequence.
        if (!await dbContext.Permission.AnyAsync(
                permission => permission.PermissionId == 2,
                cancellationToken))
        {
            if (!await dbContext.Permission.AnyAsync(cancellationToken))
            {
                dbContext.Permission.Add(new Permission
                {
                    PermissionTitle = "Media test placeholder permission"
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.Permission.Add(new Permission
            {
                PermissionTitle = "Media test teacher permission"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Permission.AnyAsync(
                permission => permission.PermissionId == 2,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The test database could not seed the permission required by Teacher.");
        }

        dbContext.UsersRoles.AddRange(
            new UsersRole { UserId = owner.UserId, RoleId = role.RoleId },
            new UsersRole { UserId = otherTeacher.UserId, RoleId = role.RoleId });

        dbContext.RolePermission.Add(new RolePermission
        {
            RoleId = role.RoleId,
            PermissionId = 2
        });

        var courseGroup = new CourseGroup
        {
            GroupTitle = $"Media test group {uniqueKey}",
            IsDelete = false
        };
        var courseStatus = new CourseStatus { StatusTitle = "Media test status" };
        var courseLevel = new CourseLevel { LevelTitle = "Media test level" };

        dbContext.CourseGroup.Add(courseGroup);
        dbContext.CourseStatus.Add(courseStatus);
        dbContext.CourseLevels.Add(courseLevel);
        await dbContext.SaveChangesAsync(cancellationToken);

        var plan = new Plan
        {
            UserId = owner.UserId,
            Users = owner,
            PlanLevel = $"Media test plan {uniqueKey}",
            PlanPrice = 0,
            Description = "Test-only course plan",
            IsDelete = false
        };
        dbContext.Plans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var course = new Course
        {
            GroupId = courseGroup.GroupId,
            TeacherId = owner.UserId,
            StatusId = courseStatus.StatusId,
            PlanId = plan.PlanId,
            LevelId = courseLevel.LevelId,
            CourseTitle = $"Media test course {uniqueKey}",
            CourseDescription = "Test-only course",
            CoursePrice = 100,
            ShortKey = uniqueKey[..5],
            Tags = "media,test",
            CourseImageName = "media-test.jpg",
            DemoFileName = "media-test.mp4",
            CreateDate = DateTime.UtcNow,
            IsDelete = false
        };

        dbContext.Course.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaTeacherCourseScenario(
            owner.UserName,
            otherTeacher.UserName,
            course.CourseId);
    }

    private static Users CreateUser(string userName) =>
        new()
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "test-only-not-a-real-password",
            UserImage = string.Empty,
            ActiveCode = string.Empty,
            RegisterDate = DateTime.UtcNow,
            IsActive = true,
            IsDelete = false
        };
}
