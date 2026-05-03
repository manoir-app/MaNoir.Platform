import * as React from 'react';
import { cx } from '../lib/cx';
import styles from './DataTable.module.css';

export interface DataTableColumn<Row> {
  id: string;
  header: React.ReactNode;
  cell: (row: Row, index: number) => React.ReactNode;
  align?: 'left' | 'center' | 'right';
  width?: number | string;
  mono?: boolean;
}

export interface DataTableProps<Row> extends Omit<React.HTMLAttributes<HTMLDivElement>, 'children'> {
  columns: DataTableColumn<Row>[];
  rows: Row[];
  rowKey: (row: Row, index: number) => React.Key;
  caption?: React.ReactNode;
  emptyState?: React.ReactNode;
}

export function DataTable<Row>({
  caption,
  className,
  columns,
  emptyState,
  rowKey,
  rows,
  ...props
}: DataTableProps<Row>) {
  return (
    <div className={cx(styles.wrapper, className)} {...props}>
      <table className={styles.table}>
        {caption ? <caption className={styles.caption}>{caption}</caption> : null}
        <thead>
          <tr>
            {columns.map((column) => {
              return (
                <th
                  className={cx(styles.head, styles[column.align ?? 'left'], column.mono && styles.mono)}
                  key={column.id}
                  scope="col"
                >
                  {column.header}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td className={styles.empty} colSpan={columns.length}>
                {emptyState ?? 'Aucune donnée.'}
              </td>
            </tr>
          ) : (
            rows.map((row, index) => (
              <tr className={styles.row} key={rowKey(row, index)}>
                {columns.map((column) => (
                  <td className={cx(styles.cell, styles[column.align ?? 'left'], column.mono && styles.mono)} key={column.id}>
                    {column.cell(row, index)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}