import js from "@eslint/js";
import ts from "@typescript-eslint/eslint-plugin";
import tsParser from "@typescript-eslint/parser";
import unusedImports from "eslint-plugin-unused-imports";
import simpleImportSort from "eslint-plugin-simple-import-sort";

export default [
  {
    files: ["src/**/*.ts"],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        project: "./tsconfig.json"
      }
    },
    plugins: {
      "@typescript-eslint": ts,
      "unused-imports": unusedImports,
      "simple-import-sort": simpleImportSort
    },
    rules: {
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          "argsIgnorePattern": "^_",
          "varsIgnorePattern": "^_",
          "destructuredArrayIgnorePattern": "^_",
          "caughtErrorsIgnorePattern": "^_"
        }
      ],
      "unused-imports/no-unused-imports": "error",
      "unused-imports/no-unused-vars": ["error", { "varsIgnorePattern": "^_", "argsIgnorePattern": "^_" }],

      // Custom Import Sorting
      "simple-import-sort/imports": [
        "error",
        {
          "groups": [
            // Built-in and external modules (Angular, RxJS, etc.)
            ["^@angular", "^rxjs", "^@?\\w"],

            // Modules from `app/core`
            ["^@app/core"],

            // Modules from `app/shared`
            ["^@app/shared"],

            // Angular Components
            ["^.*\\.component$"],

            // Angular Services
            ["^.*\\.service$"],

            // Angular Pipes
            ["^.*\\.pipe$"],

            // Angular Directives
            ["^.*\\.directive$"],

            // Relative imports (parent, sibling, index)
            ["^\\.\\./", "^\\./"]
          ]
        }
      ],

      // Ensure exports are also sorted
      "simple-import-sort/exports": "error"
    }
  }
];
