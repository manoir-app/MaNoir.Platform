declare module '@manoir-app/core-admin-ui-kit' {
  import * as React from 'react';

  export interface AttentionPanelProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
    eyebrow?: React.ReactNode;
    title: React.ReactNode;
    description?: React.ReactNode;
    actions?: React.ReactNode;
  }

  export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: 'primary' | 'secondary' | 'quiet' | 'danger';
    size?: 'sm' | 'md' | 'lg';
  }

  export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
    tone?: 'default' | 'attention';
  }

  export interface EmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
    eyebrow?: React.ReactNode;
    heading: React.ReactNode;
    description?: React.ReactNode;
    actions?: React.ReactNode;
  }

  export interface FieldProps extends React.HTMLAttributes<HTMLDivElement> {
    label?: React.ReactNode;
    hint?: React.ReactNode;
    error?: React.ReactNode;
    required?: boolean;
    htmlFor?: string;
  }

  export interface PageHeaderProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
    eyebrow?: React.ReactNode;
    title: React.ReactNode;
    description?: React.ReactNode;
    meta?: React.ReactNode;
  }

  export interface TextFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
    invalid?: boolean;
  }

  export const AttentionPanel: React.FC<AttentionPanelProps>;
  export const Button: React.FC<ButtonProps>;
  export const Card: React.FC<CardProps>;
  export const EmptyState: React.FC<EmptyStateProps>;
  export const Field: React.FC<FieldProps>;
  export const PageHeader: React.FC<PageHeaderProps>;
  export const TextField: React.ForwardRefExoticComponent<TextFieldProps & React.RefAttributes<HTMLInputElement>>;
}