## Tests 

Some tests need test setup or test containers which are slow. Run the tests like this to skip these tests.

```bash
dotnet test --filter "Category!=Dependencies"
```

## Best Practices

- Code style is enforced by StyleCop (`backend/stylecop.json`) and `.editorconfig` — follow the surrounding file's conventions.
- Do not write XML comments.
- Do write precise short comments and only when needed.
- Do not comment a class or a method, only put comments inside functions or above variables.