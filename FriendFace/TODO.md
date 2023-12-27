Create integration tests for the following features:
- [x] User can create a new account
- [x] User can log in
- [x] User can create a new post
- [x] User can edit their own post
- [x] User can delete their own post
- [x] User can like a post
- [x] User can comment on a post 

Create browser tests for the following features (Fuck UI Tests?: Find out...):
- [x] Guest can view posts on the homepage
- [x] User can register: Test the registration process from the browser's perspective. This includes filling out the registration form and submitting it.
    - Verify that the user is redirected to the homepage.
- [x] User can view their own profile: Test the process of viewing the user's own profile. This includes navigating to the profile page and checking that all expected information is present.
    - Verify that the user's name, username, and other details (?) are displayed correctly.
- [x] User can create a new post: Test the process of creating a new post. This includes navigating to the post creation page, filling out the post form, and submitting it.
  - Verify that the post is displayed on the user's profile page.
- [x] User can edit their own post: Test the process of editing a post. This includes navigating to the post, clicking the edit button, making changes to the post, and submitting the changes.
  - Verify that the post is updated correctly.
- [x] User can delete their own post: Test the process of deleting a post. This includes navigating to the post, clicking the delete button, and confirming the deletion.  
  - Verify that the post is no longer displayed on the page.
- [x] User can like a post: Test the process of liking a post. This includes navigating to the post and clicking the like button.
  - Verify that the post's like count is updated correctly, and that the heart turns red.
- [x] User can comment on a post: Test the process of commenting on a post. This includes navigating to the post, entering a comment, and submitting it.
- [x] User can log out: Test the process of logging out. This includes clicking the logout button and confirming that the user is logged out.
9. **Check Visual Elements**: In addition to functionality, browser tests should also check visual elements like layout, colors, fonts, etc. This can help catch visual regressions.

10. **Keep Tests Maintainable**: As your application grows, so will your tests. It's important to keep them well-organized and easy to update. This often involves using good naming conventions, keeping tests small and focused, and avoiding duplication.

Remember, the goal of testing is to increase confidence in your application. The exact practices you follow may vary based on your specific needs and context.
