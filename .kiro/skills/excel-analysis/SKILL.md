---
name: excel-analysis
description: Analyze, manipulate, and transform Excel files (.xlsx, .xls). Use when reading spreadsheets, performing data analysis, numerical computations, filtering, aggregating, modifying cell values, copying workbooks, or writing results to new Excel files. Relevant for tasks involving openpyxl, pandas, and spreadsheet data processing.
---

## Required Python Packages

Before any Excel operation, verify that the required packages are installed:

1. **openpyxl** - For reading/writing Excel .xlsx files, cell-level manipulation, formatting, and workbook copying
2. **pandas** - For tabular data analysis, filtering, aggregation, pivoting, and numerical computation

Run this check first:
```bash
pip show openpyxl pandas
```

If either package is missing, install it:
```bash
pip install openpyxl pandas
```

## Workflow

Follow these steps in order for every Excel task:

### Step 1 - Verify environment

Confirm `openpyxl` and `pandas` are importable. If not, install them with `pip install openpyxl pandas`.

### Step 2 - Read the input Excel file(s)

- Use `pandas.read_excel()` for data analysis workflows (returns a DataFrame).
- Use `openpyxl.load_workbook()` when you need cell-level access, formatting, or structural manipulation.
- Always inspect sheet names, shape, columns, and dtypes before proceeding.
- Handle multi-sheet workbooks by listing sheets and letting the user choose, or processing all sheets.

### Step 3 - Perform data and numerical analysis

- Use pandas for filtering, grouping, aggregation, pivot tables, statistical summaries, and computed columns.
- Use openpyxl for cell-level reads, formula inspection, or format-aware operations.
- Print or display intermediate results so the user can verify before proceeding.

### Step 4 - Prepare the result dataset

- Build the final DataFrame or workbook structure.
- Validate data types, handle missing values, and confirm the output schema with the user if ambiguous.

### Step 5 - Write output

Choose the appropriate strategy:

**Modifying existing data (same layout):**
- Copy the original file first (`shutil.copy2(original, new_path)`) to preserve formatting, formulas, and styles.
- Open the copy with `openpyxl.load_workbook()` and apply changes in place.
- Save the copy. Never overwrite the original file.

**Producing a structurally different result:**
- Write to a brand-new Excel file using `pandas.DataFrame.to_excel()` or `openpyxl.Workbook()`.
- Use a descriptive filename that distinguishes it from the source.

## Key Guidelines

- **Never overwrite the original file.** Always work on a copy or write to a new file.
- When using `pandas.read_excel()`, pass `engine='openpyxl'` for .xlsx files.
- For large files, consider `chunksize` or selective column reading to manage memory.
- Preserve original formatting when modifying: use openpyxl on a copied file rather than round-tripping through pandas (which strips formatting).
- When writing with pandas, use `index=False` unless the index is meaningful.
- If the user provides a file path, resolve it relative to the workspace root.
